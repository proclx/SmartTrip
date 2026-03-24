using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartTrip.Application.Interfaces;
using SmartTrip.Data;
using SmartTrip.Models;

namespace SmartTrip.Application.Services
{
    public class GalleryService : IGalleryService
    {
        private readonly SmartTripDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public GalleryService(SmartTripDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IEnumerable<Photo>> GetUserPhotosAsync(string userId)
        {
            return await _context.Photos
                .Include(p => p.Trip)          // Завантажуємо дані про подорож
                    .ThenInclude(t => t.City)  // Завантажуємо дані про місто всередині подорожі
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Trip.StartDate) // Сортуємо: нові подорожі зверху
                .ToListAsync();
        }

        public async Task<IEnumerable<Photo>> GetTripPhotosAsync(int tripId, string userId)
        {
            return await _context.Photos
                .Where(p => p.TripId == tripId && p.UserId == userId)
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();
        }

        public async Task UploadPhotosAsync(List<IFormFile> files, int tripId, string userId)
        {
            // Перевірка, чи належить подорож користувачу (безпека!)
            var tripExists = await _context.Trips
                .AnyAsync(t => t.Id == tripId && t.UserId == userId);

            if (!tripExists || files == null || files.Count == 0) return;

            // Шлях до папки: wwwroot/uploads/gallery
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "gallery");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    // Генеруємо унікальне ім'я файлу
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Зберігаємо відносний шлях у БД для відображення в тегу <img>
                    var photo = new Photo
                    {
                        FilePath = $"/uploads/gallery/{fileName}",
                        UserId = userId,
                        TripId = tripId,
                        UploadDate = DateTime.UtcNow
                    };

                    _context.Photos.Add(photo);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeletePhotoAsync(int photoId, string userId)
        {
            var photo = await _context.Photos
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId);

            if (photo != null)
            {
                // Видаляємо фізичний файл
                var fullPath = Path.Combine(_environment.WebRootPath, photo.FilePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                _context.Photos.Remove(photo);
                await _context.SaveChangesAsync();
            }
        }
    }
}
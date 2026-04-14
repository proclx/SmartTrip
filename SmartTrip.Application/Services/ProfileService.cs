using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SmartTrip.Application.Interfaces;
using SmartTrip.Data;
using SmartTrip.Models;

namespace SmartTrip.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly SmartTripDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<User> _userManager;

        public ProfileService(SmartTripDbContext context, IWebHostEnvironment environment, UserManager<User> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        public async Task<User?> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user;
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, string firstName, string lastName, string email)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            try
            {
                // Оновлюємо дані
                user.FirstName = firstName;
                user.LastName = lastName;

                var userUpdateResult = await _userManager.UpdateAsync(user);
                if (!userUpdateResult.Succeeded)
                {
                    return false;
                }

                // Якщо змінився емейл (включно з UserName)
                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailResult = await _userManager.SetEmailAsync(user, email);
                    var usernameResult = await _userManager.SetUserNameAsync(user, email);

                    if (!emailResult.Succeeded || !usernameResult.Succeeded)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UploadProfileImageAsync(string userId, IFormFile imageFile)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || imageFile == null || imageFile.Length == 0)
                return false;

            try
            {
                // Перевіряємо тип файлу
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return false;

                // Максимальний розмір 5MB
                if (imageFile.Length > 5 * 1024 * 1024)
                    return false;

                // Шлях до папки: wwwroot/uploads/profiles
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Видаляємо старе фото якщо існує
                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfileImageUrl.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                // Генеруємо унікальне ім'я файлу
                var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Оновлюємо URL в БД
                user.ProfileImageUrl = $"/uploads/profiles/{fileName}";

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Користувача не знайдено." });
            }

            return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }

        public async Task<bool> DeleteProfileImageAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            try
            {
                // Видаляємо файл
                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, user.ProfileImageUrl.TrimStart('/'));
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }

                // Очищуємо URL в БД
                user.ProfileImageUrl = null;
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }
    }
}

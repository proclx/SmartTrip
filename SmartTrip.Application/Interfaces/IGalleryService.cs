using SmartTrip.Models;
using Microsoft.AspNetCore.Http;

namespace SmartTrip.Application.Interfaces
{
    public interface IGalleryService
    {
        // Отримати всі фото користувача
        Task<IEnumerable<Photo>> GetUserPhotosAsync(string userId);

        // Отримати фото конкретної подорожі
        Task<IEnumerable<Photo>> GetTripPhotosAsync(int tripId, string userId);

        // Завантажити одне або кілька фото
        Task UploadPhotosAsync(List<IFormFile> files, int tripId, string userId);

        // Видалити фото
        Task DeletePhotoAsync(int photoId, string userId);
    }
}
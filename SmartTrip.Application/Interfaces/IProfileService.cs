using SmartTrip.Models;
using Microsoft.AspNetCore.Http;

namespace SmartTrip.Application.Interfaces
{
    public interface IProfileService
    {
        // Отримати профіль користувача
        Task<User?> GetUserProfileAsync(string userId);

        // Оновити дані профіля (ім'я, прізвище, емейл)
        Task<bool> UpdateUserProfileAsync(string userId, string firstName, string lastName, string email);

        // Завантажити фото профіля
        Task<bool> UploadProfileImageAsync(string userId, IFormFile imageFile);

        // Видалити фото профіля
        Task<bool> DeleteProfileImageAsync(string userId);
    }
}

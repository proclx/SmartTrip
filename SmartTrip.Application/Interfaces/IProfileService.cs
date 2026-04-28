using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SmartTrip.Models;

namespace SmartTrip.Application.Interfaces
{
    public interface IProfileService
    {
        // Отримати профіль користувача
        Task<User?> GetUserProfileAsync(string userId);

        // Оновити дані профіля (ім'я, прізвище, емейл, про мене)
        Task<bool> UpdateUserProfileAsync(string userId, string firstName, string lastName, string email, string? about = null);

        // Змінити пароль користувача
        Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        // Завантажити фото профіля
        Task<bool> UploadProfileImageAsync(string userId, IFormFile imageFile);

        // Видалити фото профіля
        Task<bool> DeleteProfileImageAsync(string userId);
    }
}

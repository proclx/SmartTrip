using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTrip.Models;

namespace SmartTrip.Application.Interfaces
{
    public interface ITripService
    {
        Task<int> CreateTripAsync(string userId, string destinationName, DateTime startDate, DateTime endDate);

        // метод для отримання списку подорожей користувача
        Task<IEnumerable<Trip>> GetUserTripsAsync(string userId);

        // метод для отримання улюблених подорожей користувача
        Task<IEnumerable<Trip>> GetFavoriteTripsAsync(string userId);

        // метод для отримання детальної інформації по подорожі
        Task<Trip?> GetTripByIdAsync(int tripId, string userId);

        // оновлення додаткових даних (кількість людей, рейтинг)
        Task<bool> UpdateTripAsync(int tripId, string userId, int peopleCount, int? rating);

        // переключити статус улюбленої подорожі
        Task<bool> ToggleFavoriteAsync(int tripId, string userId);
        Task<Trip> GetTripDetailsAsync(int tripId);
    }
}
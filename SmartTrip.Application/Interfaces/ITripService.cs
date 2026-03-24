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
    }
}
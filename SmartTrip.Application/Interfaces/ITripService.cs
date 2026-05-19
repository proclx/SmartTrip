using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTrip.Models;

namespace SmartTrip.Application.Interfaces
{
    public interface ITripService
    {
        Task<int> CreateTripAsync(string userId, string destinationName, string startingPoint, DateTime startDate, DateTime endDate, string? notes);

        // метод для отримання списку подорожей користувача
        Task<IEnumerable<Trip>> GetUserTripsAsync(string userId);

        // метод для отримання улюблених подорожей користувача
        Task<IEnumerable<Trip>> GetFavoriteTripsAsync(string userId);

        // метод для отримання детальної інформації по подорожі
        Task<Trip?> GetTripByIdAsync(int tripId, string userId);

        // оновлення додаткових даних (кількість людей, рейтинг, нотатки)
        Task<bool> UpdateTripAsync(int tripId, string userId, int peopleCount, int? rating, string? notes, DateTime startDate, DateTime endDate);

        // переключити статус улюбленої подорожі
        Task<bool> ToggleFavoriteAsync(int tripId, string userId);

        Task<Trip> GetTripDetailsAsync(int tripId);

        Task<ItineraryItem?> GetItineraryItemByIdAsync(int itemId);
        Task<bool> UpdateItineraryItemAsync(int itemId, string newTitle, string newDescription, TimeSpan? newTime);
        Task<bool> DeleteItineraryItemAsync(int itemId);
        Task<IEnumerable<Trip>> GetArchivedTripsAsync(string userId);
        Task<bool> ToggleArchiveAsync(int tripId, string userId);
        Task<bool> ApplyRainModeToDayAsync(int tripDayId, string userId);

        Task<int> CloneTripAsync(int tripId, string userId);

        Task UpdateDayItineraryOrderAsync(int dayId, List<int> orderedItemIds);

        Task<VotingSession> StartVotingSessionAsync(int tripId, int peopleCount, string preferences);
        Task<VotingSession> GetVotingSessionAsync(Guid shareToken);
        Task SubmitVoteAsync(Guid shareToken, int votingItemId, string voterId, bool isLiked);
        Task TryFinalizeVotingAsync(Guid shareToken);
    }
}
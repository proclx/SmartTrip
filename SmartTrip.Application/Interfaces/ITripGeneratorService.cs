using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTrip.Models;

namespace SmartTrip.Application.Interfaces
{
    public interface ITripGeneratorService
    {
        Task GenerateItineraryAsync(int tripId);

        /// <summary>
        /// Prompts the AI to replace outdoor activities with indoor alternatives, keeping meal times intact.
        /// </summary>
        Task<List<ItineraryItem>> AdaptForRainModeAsync(string destinationCity, List<ItineraryItem> originalItems);

        /// <summary>
        /// Генерує великий пул цікавих локацій для голосування друзів.
        /// </summary>
        Task<List<VotingItem>> GenerateVotingPoolAsync(string cityName, int daysCount, string preferences);

        /// <summary>
        /// Складає фінальний маршрут використовуючи лише ті локації, за які проголосувала більшість.
        /// </summary>
        Task<List<ItineraryItem>> FinalizeItineraryFromVotesAsync(string cityName, int daysCount, List<VotingItem> approvedItems);
    }
}

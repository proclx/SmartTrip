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
    }
}

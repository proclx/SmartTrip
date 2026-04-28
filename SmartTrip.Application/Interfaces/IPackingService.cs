using SmartTrip.Models;

namespace SmartTrip.Application.Interfaces
{
    public interface IPackingService
    {
        // --- ДЕФОЛТНИЙ СПИСОК ---
        Task<IEnumerable<DefaultPackingItem>> GetDefaultItemsAsync(string userId);
        Task<DefaultPackingItem> AddDefaultItemAsync(string userId, string name, string category);
        Task UpdateDefaultItemAsync(int itemId, string name, string category, string userId);
        Task DeleteDefaultItemAsync(int itemId, string userId);

        // --- СПИСОК ДЛЯ ПОДОРОЖІ ---
        Task<IEnumerable<TripPackingItem>> GetTripItemsAsync(int tripId, string userId);
        Task<TripPackingItem> AddTripItemAsync(int tripId, string name, string category, string userId);
        Task DeleteTripItemAsync(int itemId, string userId);
        Task ToggleItemStatusAsync(int itemId, string userId);

        

        // 1. Копіює дефолтний список у подорож 
        Task InitializeTripListAsync(int tripId, string userId);

        // 2. Додає з дефолтного списку ті речі, яких ще немає в подорожі
        Task SyncWithDefaultListAsync(int tripId, string userId);

        // 3. Видаляє все з подорожі і копіює дефолтний список наново
        Task ResetToDefaultListAsync(int tripId, string userId);
    }
}
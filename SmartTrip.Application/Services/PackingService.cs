using Microsoft.EntityFrameworkCore;
using SmartTrip.Application.Interfaces;
using SmartTrip.Data;
using SmartTrip.Models;

namespace SmartTrip.Application.Services
{
    public class PackingService : IPackingService
    {
        private readonly SmartTripDbContext _context;

        public PackingService(SmartTripDbContext context)
        {
            _context = context;
        }

        // --- ДЕФОЛТНИЙ СПИСОК ---

        public async Task<IEnumerable<DefaultPackingItem>> GetDefaultItemsAsync(string userId)
        {
            return await _context.DefaultPackingItems
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Category).ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<DefaultPackingItem> AddDefaultItemAsync(string userId, string name, string category)
        {
            var item = new DefaultPackingItem
            {
                UserId = userId,
                Name = name,
                Category = string.IsNullOrWhiteSpace(category) ? "Інше" : category
            };

            _context.DefaultPackingItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task UpdateDefaultItemAsync(int itemId, string name, string category, string userId)
        {
            var item = await _context.DefaultPackingItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.UserId == userId);

            if (item != null)
            {
                item.Name = name;
                item.Category = string.IsNullOrWhiteSpace(category) ? "Інше" : category;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteDefaultItemAsync(int itemId, string userId)
        {
            var item = await _context.DefaultPackingItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.UserId == userId);

            if (item != null)
            {
                _context.DefaultPackingItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        // --- СПИСОК ДЛЯ ПОДОРОЖІ ---

        public async Task<IEnumerable<TripPackingItem>> GetTripItemsAsync(int tripId, string userId)
        {
            // Перевіряємо безпеку (чи належить подорож юзеру)
            var tripExists = await _context.Trips.AnyAsync(t => t.Id == tripId && t.UserId == userId);
            if (!tripExists) return new List<TripPackingItem>();

            return await _context.TripPackingItems
                .Where(x => x.TripId == tripId)
                .OrderBy(x => x.Category).ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<TripPackingItem> AddTripItemAsync(int tripId, string name, string category, string userId)
        {
            var tripExists = await _context.Trips.AnyAsync(t => t.Id == tripId && t.UserId == userId);
            if (!tripExists) return null!;

            var item = new TripPackingItem
            {
                TripId = tripId,
                Name = name,
                Category = string.IsNullOrWhiteSpace(category) ? "Інше" : category,
                IsChecked = false
            };

            _context.TripPackingItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task DeleteTripItemAsync(int itemId, string userId)
        {
            // Include Trip, щоб перевірити UserId
            var item = await _context.TripPackingItems
                .Include(x => x.Trip)
                .FirstOrDefaultAsync(x => x.Id == itemId && x.Trip!.UserId == userId);

            if (item != null)
            {
                _context.TripPackingItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ToggleItemStatusAsync(int itemId, string userId)
        {
            var item = await _context.TripPackingItems
                .Include(x => x.Trip)
                .FirstOrDefaultAsync(x => x.Id == itemId && x.Trip!.UserId == userId);

            if (item != null)
            {
                item.IsChecked = !item.IsChecked; // Інвертуємо статус
                await _context.SaveChangesAsync();
            }
        }

        // --- КІЛЕР-ФІЧІ ---

        public async Task InitializeTripListAsync(int tripId, string userId)
        {
            var defaultItems = await GetDefaultItemsAsync(userId);
            if (!defaultItems.Any()) return;

            var tripItems = defaultItems.Select(d => new TripPackingItem
            {
                TripId = tripId,
                Name = d.Name,
                Category = d.Category,
                IsChecked = false
            });

            _context.TripPackingItems.AddRange(tripItems);
            await _context.SaveChangesAsync();
        }

        public async Task SyncWithDefaultListAsync(int tripId, string userId)
        {
            var defaultItems = await GetDefaultItemsAsync(userId);
            var currentTripItems = await GetTripItemsAsync(tripId, userId);

            // Знаходимо речі з дефолтного списку, яких за Назвою ще немає в цій подорожі
            var existingNames = currentTripItems.Select(x => x.Name.ToLower()).ToHashSet();

            var itemsToAdd = defaultItems
                .Where(d => !existingNames.Contains(d.Name.ToLower()))
                .Select(d => new TripPackingItem
                {
                    TripId = tripId,
                    Name = d.Name,
                    Category = d.Category,
                    IsChecked = false
                });

            if (itemsToAdd.Any())
            {
                _context.TripPackingItems.AddRange(itemsToAdd);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResetToDefaultListAsync(int tripId, string userId)
        {
            var currentTripItems = await _context.TripPackingItems
                .Include(x => x.Trip)
                .Where(x => x.TripId == tripId && x.Trip!.UserId == userId)
                .ToListAsync();

            _context.TripPackingItems.RemoveRange(currentTripItems);
            await _context.SaveChangesAsync(); // Видаляємо старі

            await InitializeTripListAsync(tripId, userId); // Копіюємо нові
        }
    }
}
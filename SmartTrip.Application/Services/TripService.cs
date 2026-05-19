using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SmartTrip.Data;
using SmartTrip.Models;
using SmartTrip.Application.Interfaces;

namespace SmartTrip.Application.Services
{
    public class TripService : ITripService
    {
        private readonly SmartTripDbContext _context;
        private readonly ITripGeneratorService _tripGeneratorService;
        private readonly IPackingService _packingService;
        private readonly IMemoryCache _memoryCache;
        private readonly int _referenceDataCacheMinutes;

        public TripService(
            SmartTripDbContext context,
            ITripGeneratorService tripGeneratorService,
            IPackingService packingService,
            IMemoryCache memoryCache,
            IConfiguration configuration)
        {
            _context = context;
            _tripGeneratorService = tripGeneratorService;
            _packingService = packingService;
            _memoryCache = memoryCache;
            _referenceDataCacheMinutes = configuration.GetValue<int>("CacheSettings:ReferenceDataCacheMinutes", 60);
        }

        public async Task<int> CreateTripAsync(string userId, string destinationName, string startingPoint, DateTime startDate, DateTime endDate, string? notes)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Trips\" ADD COLUMN IF NOT EXISTS \"StartingPoint\" text;");
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Trips\" ADD COLUMN IF NOT EXISTS \"RouteToDestination\" text;");
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Trips\" ADD COLUMN IF NOT EXISTS \"RouteBack\" text;");
                await _context.Database.ExecuteSqlRawAsync("INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260401000000_AddStartingPointAndRoutes', '8.0.0') ON CONFLICT DO NOTHING;");
            }
            catch { }

            string normalizedDestination = destinationName.Trim().ToLowerInvariant();
            string cityCacheKey = $"city:{normalizedDestination}";

            int cityId;

            if (_memoryCache.TryGetValue(cityCacheKey, out int cachedCityId))
            {
                cityId = cachedCityId;
            }
            else
            {
                var city = await _context.Cities
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == normalizedDestination);

                if (city == null)
                {
                    city = new City
                    {
                        Name = destinationName,
                        Country = "Не вказано"
                    };

                    _context.Cities.Add(city);
                    await _context.SaveChangesAsync();
                }

                cityId = city.Id;

                _memoryCache.Set(cityCacheKey, cityId, TimeSpan.FromMinutes(_referenceDataCacheMinutes));
            }

            var trip = new Trip
            {
                UserId = userId,
                CityId = cityId,
                StartDate = startDate.ToUniversalTime(),
                EndDate = endDate.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow,
                StartingPoint = startingPoint,
                Notes = notes
            };

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            int totalDays = (endDate.Date - startDate.Date).Days + 1;

            for (int i = 0; i < totalDays; i++)
            {
                var tripDay = new TripDay
                {
                    TripId = trip.Id,
                    Date = startDate.AddDays(i).ToUniversalTime(),
                    DayNumber = i + 1
                };

                _context.TripDays.Add(tripDay);
            }

            await _context.SaveChangesAsync();

            await _tripGeneratorService.GenerateItineraryAsync(trip.Id);
            await _packingService.InitializeTripListAsync(trip.Id, userId);

            return trip.Id;
        }

        public async Task<IEnumerable<Trip>> GetUserTripsAsync(string userId)
        {
            return await _context.Trips
                .Include(t => t.City)
                .Include(t => t.Photos)
                .Where(t => t.UserId == userId && !t.IsArchived)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Trip>> GetFavoriteTripsAsync(string userId)
        {
            return await _context.Trips
                .Include(t => t.City)
                .Include(t => t.Photos)
                .Where(t => t.UserId == userId && t.IsFavorite)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<Trip?> GetTripByIdAsync(int tripId, string userId)
        {
            return await _context.Trips
                .Include(t => t.City)
                .Include(t => t.Photos)
                .FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId);
        }

        public async Task<bool> UpdateTripAsync(int tripId, string userId, int peopleCount, int? rating, string? notes, DateTime startDate, DateTime endDate)
        {
            var trip = await _context.Trips
                .Include(t => t.TripDays)
                .FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId);

            if (trip == null) return false;

            trip.PeopleCount = peopleCount;
            trip.Rating = rating;
            trip.Notes = notes;

            if (trip.StartDate.Date != startDate.Date || trip.EndDate.Date != endDate.Date)
            {
                trip.StartDate = startDate.ToUniversalTime();
                trip.EndDate = endDate.ToUniversalTime();

                int newTotalDays = (endDate.Date - startDate.Date).Days + 1;
                var existingDays = trip.TripDays.OrderBy(d => d.DayNumber).ToList();

                for (int i = 0; i < Math.Min(newTotalDays, existingDays.Count); i++)
                {
                    existingDays[i].Date = startDate.AddDays(i).ToUniversalTime();
                }

                if (newTotalDays > existingDays.Count)
                {
                    for (int i = existingDays.Count; i < newTotalDays; i++)
                    {
                        _context.TripDays.Add(new TripDay
                        {
                            TripId = trip.Id,
                            Date = startDate.AddDays(i).ToUniversalTime(),
                            DayNumber = i + 1
                        });
                    }
                }
                else if (newTotalDays < existingDays.Count)
                {
                    var daysToRemove = existingDays.Skip(newTotalDays).ToList();
                    _context.TripDays.RemoveRange(daysToRemove);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleFavoriteAsync(int tripId, string userId)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId);
            if (trip == null)
                return false;

            trip.IsFavorite = !trip.IsFavorite;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Trip> GetTripDetailsAsync(int tripId)
        {
            return await _context.Trips
                .Include(t => t.City)
                .Include(t => t.TripDays.OrderBy(d => d.DayNumber))
                    .ThenInclude(d => d.ItineraryItems.OrderBy(i => i.StartTime))
                        .ThenInclude(i => i.Place)
                .FirstOrDefaultAsync(t => t.Id == tripId);
        }

        public async Task<ItineraryItem?> GetItineraryItemByIdAsync(int itemId)
        {
            return await _context.ItineraryItems // Або як називається ваш DbSet для елементів розкладу
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<bool> UpdateItineraryItemAsync(int itemId, string newTitle, string newDescription, TimeSpan? newTime)
        {
            var item = await _context.ItineraryItems.FindAsync(itemId);
            if (item == null) return false;

            item.Notes = newDescription;

            if (newTime.HasValue)
            {
                item.StartTime = newTime.Value;
            }

            _context.ItineraryItems.Update(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteItineraryItemAsync(int itemId)
        {
            var item = await _context.ItineraryItems.FindAsync(itemId);
            if (item == null) return false;

            _context.ItineraryItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Trip>> GetArchivedTripsAsync(string userId)
        {
            return await _context.Trips
                .Include(t => t.City)
                .Include(t => t.Photos)
                .Where(t => t.UserId == userId && t.IsArchived)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<bool> ToggleArchiveAsync(int tripId, string userId)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId);
            if (trip == null) return false;

            trip.IsArchived = !trip.IsArchived;
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> ApplyRainModeToDayAsync(int tripDayId, string userId)
        {
            var tripDay = await _context.TripDays
                .Include(td => td.Trip)
                    .ThenInclude(t => t.City) 
                .Include(td => td.ItineraryItems)
                    .ThenInclude(i => i.Place) 
                .FirstOrDefaultAsync(td => td.Id == tripDayId && td.Trip.UserId == userId);

            if (tripDay == null || tripDay.ItineraryItems == null || !tripDay.ItineraryItems.Any())
            {
                return false;
            }

            var adaptedItinerary = await _tripGeneratorService.AdaptForRainModeAsync(
                tripDay.Trip.City?.Name ?? string.Empty,
                tripDay.ItineraryItems.ToList()
            );
 
            if (adaptedItinerary == null || !adaptedItinerary.Any())
            {
                return false; 
            }

            _context.ItineraryItems.RemoveRange(tripDay.ItineraryItems);
            await _context.SaveChangesAsync();

            // Apply new indoor schedule
            foreach (var item in adaptedItinerary)
            {
                item.Id = 0; 
                item.TripDayId = tripDayId;
                _context.ItineraryItems.Add(item);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CloneTripAsync(int tripId, string userId)
        {
            // 1. Отримуємо оригінальну подорож з усіма днями та пунктами маршруту
            var originalTrip = await _context.Trips
                .Include(t => t.TripDays)
                    .ThenInclude(d => d.ItineraryItems)
                .FirstOrDefaultAsync(t => t.Id == tripId && t.UserId == userId);

            if (originalTrip == null) return 0;

            // 2. Створюємо копію подорожі
            var clonedTrip = new Trip
            {
                UserId = userId,
                CityId = originalTrip.CityId,
                StartingPoint = originalTrip.StartingPoint,
                StartDate = originalTrip.StartDate,
                EndDate = originalTrip.EndDate,
                PeopleCount = originalTrip.PeopleCount,
                Notes = originalTrip.Notes,
                CreatedAt = DateTime.UtcNow,
                IsFavorite = false,
                IsArchived = false
            };

            _context.Trips.Add(clonedTrip);
            await _context.SaveChangesAsync(); // Отримуємо новий Id для clonedTrip

            // 3. Копіюємо дні та пункти маршруту
            foreach (var oldDay in originalTrip.TripDays.OrderBy(d => d.DayNumber))
            {
                var newDay = new TripDay
                {
                    TripId = clonedTrip.Id,
                    Date = oldDay.Date,
                    DayNumber = oldDay.DayNumber
                };
                _context.TripDays.Add(newDay);
                await _context.SaveChangesAsync(); // Отримуємо Id нового дня для ItineraryItems

                foreach (var oldItem in oldDay.ItineraryItems)
                {
                    var newItem = new ItineraryItem
                    {
                        TripDayId = newDay.Id,
                        PlaceId = oldItem.PlaceId,
                        StartTime = oldItem.StartTime,
                        EndTime = oldItem.EndTime,
                        Notes = oldItem.Notes
                    };
                    _context.ItineraryItems.Add(newItem);
                }
            }

            await _context.SaveChangesAsync();

            // 4. Ініціалізуємо чеклист для нової подорожі 
            await _packingService.InitializeTripListAsync(clonedTrip.Id, userId);

            return clonedTrip.Id;
        }

        public async Task UpdateDayItineraryOrderAsync(int dayId, List<int> orderedItemIds)
        {
            var day = await _context.TripDays
                .Include(d => d.ItineraryItems)
                .FirstOrDefaultAsync(d => d.Id == dayId);

            if (day == null) throw new Exception("Day not found");

            for (int i = 0; i < orderedItemIds.Count; i++)
            {
                var item = day.ItineraryItems.FirstOrDefault(xi => xi.Id == orderedItemIds[i]);
                if (item != null)
                {
                    item.OrderOffset = i; 
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
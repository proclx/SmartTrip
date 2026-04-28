using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SmartTrip.Application.Interfaces;
using SmartTrip.Data;
using SmartTrip.Infrastructure;
using SmartTrip.Models;
using SmartTrip.Models.Enum;

namespace SmartTrip.Application.Services
{
    // Спеціальний клас для читання відповіді від ШІ
    public class GeminiRouteItem
    {
        public int DayIndex { get; set; }
        public string PlaceName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class TripGeneratorService : ITripGeneratorService
    {
        private readonly SmartTripDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IMemoryCache _memoryCache;
        private readonly int _tripGenerationCacheMinutes;

        private class CachedTripGenerationData
        {
            public List<GeminiRouteItem> Items { get; set; } = new();
            public string? RouteToDestination { get; set; }
            public string? RouteBack { get; set; }
        }

        public TripGeneratorService(
            SmartTripDbContext context,
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _context = context;
            _httpClient = httpClient;
            _memoryCache = memoryCache;

            _httpClient.Timeout = TimeSpan.FromMinutes(3);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SmartTripApp/1.0");

            _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new Exception("API ключ не знайдено!");
            _tripGenerationCacheMinutes = configuration.GetValue<int>("CacheSettings:TripGenerationCacheMinutes", 60);
        }

        public async Task GenerateItineraryAsync(int tripId)
        {
            var trip = await _context.Trips
                .Include(t => t.City)
                .Include(t => t.TripDays)
                .FirstOrDefaultAsync(t => t.Id == tripId);

            if (trip == null || trip.City == null || !trip.TripDays.Any()) return;

            int totalDays = trip.TripDays.Count;
            string cityName = trip.City.Name;
            string tripCacheKey = BuildTripGenerationCacheKey(cityName, totalDays, trip.StartingPoint);

            if (_memoryCache.TryGetValue(tripCacheKey, out CachedTripGenerationData? cachedData) && cachedData != null)
            {
                await ApplyGeneratedDataToTripAsync(trip, cachedData.Items, cachedData.RouteToDestination, cachedData.RouteBack);
                return;
            }

            // 2. Формуємо строгий промпт для Gemini
            string prompt = $@"
                Створи реалістичний туристичний маршрут на {totalDays} днів для міста {cityName}.
                Поверни ТІЛЬКИ чистий масив JSON (без форматування markdown, без ```json на початку).
                Формат кожного об'єкта в масиві має бути строго таким:
                [
                  {{
                    ""DayIndex"": 1,
                    ""PlaceName"": ""Назва реального готелю, ресторану або пам'ятки"",
                    ""Category"": ""Attraction"", // Використовуй строго одне з трьох: ""Hotel"", ""Restaurant"", або ""Attraction""
                    ""StartTime"": ""10:00:00"",
                    ""EndTime"": ""12:00:00"",
                    ""Notes"": ""Короткий опис, що там робити""
                  }}
                ]
                Склади логічний розклад з 09:00 до 21:00 на кожен день ({totalDays} днів). Включи сніданок, обід, вечерю та пам'ятки.
            ";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 1. Очищаємо ключ від можливих прихованих пробілів чи перенесень рядка
            string cleanApiKey = _apiKey.Trim();

            // 2. Формуємо правильний абсолютний URI (переконайся, що https:// на місці)
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={cleanApiKey}";

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return;

            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseString);
                var aiText = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                if (string.IsNullOrEmpty(aiText)) return;

                aiText = aiText.Replace("```json", "").Replace("```", "").Trim();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var generatedItems = JsonSerializer.Deserialize<List<GeminiRouteItem>>(aiText, options);

                if (generatedItems == null) return;

                string? routeToDestination = null;
                string? routeBack = null;

                // Generate routes if starting point is provided
                if (!string.IsNullOrEmpty(trip.StartingPoint))
                {
                    routeToDestination = await GenerateRouteAsync(trip.StartingPoint, trip.City.Name);
                    routeBack = await GenerateRouteAsync(trip.City.Name, trip.StartingPoint);
                }

                await ApplyGeneratedDataToTripAsync(trip, generatedItems, routeToDestination, routeBack);

                _memoryCache.Set(tripCacheKey, new CachedTripGenerationData
                {
                    Items = generatedItems,
                    RouteToDestination = routeToDestination,
                    RouteBack = routeBack
                }, TimeSpan.FromMinutes(_tripGenerationCacheMinutes));
            }
            catch (Exception ex)
            {
                // Тут можна додати логування, якщо ШІ поверне невалідний JSON
                Console.WriteLine($"Помилка генерації маршруту: {ex.Message}");
            }
        }

        private async Task<string?> GenerateRouteAsync(string from, string to)
        {
            string prompt = $@"
Опишіть коротко основні способи добратися від {from} до {to}. Включіть:
- Літак (якщо далеко)
- Автомобіль/автобус
- Потяг
- Приблизний час та відстань

Поверніть відповідь українською мовою у форматі:
🚗 **Автомобіль:** [короткий опис]
✈️ **Літак:** [короткий опис]  
🚂 **Потяг:** [короткий опис]
";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            string cleanApiKey = _apiKey.Trim();
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={cleanApiKey}";

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return null;

            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseString);
                var aiText = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                return aiText?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static string BuildTripGenerationCacheKey(string cityName, int totalDays, string? startingPoint)
        {
            string normalizedCity = cityName.Trim().ToLowerInvariant();
            string normalizedStart = (startingPoint ?? string.Empty).Trim().ToLowerInvariant();
            return $"trip-generation:{normalizedCity}:{totalDays}:{normalizedStart}";
        }

        private async Task ApplyGeneratedDataToTripAsync(Trip trip, List<GeminiRouteItem> generatedItems, string? routeToDestination, string? routeBack)
        {
            var tripDayIds = trip.TripDays.Select(d => d.Id).ToList();
            var existingItems = await _context.ItineraryItems
                .Where(i => tripDayIds.Contains(i.TripDayId))
                .ToListAsync();

            if (existingItems.Any())
            {
                _context.ItineraryItems.RemoveRange(existingItems);
            }

            var cityPlaces = await _context.Places
                .Where(p => p.CityId == trip.CityId)
                .ToListAsync();

            var placesByName = cityPlaces
                .GroupBy(p => p.Name.ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var item in generatedItems)
            {
                var normalizedPlaceName = item.PlaceName.Trim().ToLowerInvariant();

                if (!placesByName.TryGetValue(normalizedPlaceName, out var place))
                {
                    PlaceType parsedType = PlaceType.Attraction;
                    Enum.TryParse(item.Category, true, out parsedType);

                    place = new Place
                    {
                        CityId = trip.CityId,
                        Name = item.PlaceName,
                        Type = parsedType,
                        Rating = 4.5
                    };

                    _context.Places.Add(place);
                    await _context.SaveChangesAsync();
                    placesByName[normalizedPlaceName] = place;
                }

                var tripDay = trip.TripDays.FirstOrDefault(d => d.DayNumber == item.DayIndex);
                if (tripDay == null)
                {
                    continue;
                }

                var itineraryItem = new ItineraryItem
                {
                    TripDayId = tripDay.Id,
                    PlaceId = place.Id,
                    StartTime = TimeSpan.Parse(item.StartTime),
                    EndTime = TimeSpan.Parse(item.EndTime),
                    Notes = item.Notes
                };

                _context.ItineraryItems.Add(itineraryItem);
            }

            trip.RouteToDestination = routeToDestination;
            trip.RouteBack = routeBack;

            await _context.SaveChangesAsync();
        }

        public async Task<List<ItineraryItem>> AdaptForRainModeAsync(string destinationCity, List<ItineraryItem> originalItems)
        {
            if (originalItems == null || originalItems.Count == 0)
            {
                return new List<ItineraryItem>();
            }

            // Формуємо контекст з поточним розкладом для ШІ
            var originalSchedule = originalItems.Select(i => new 
            {
                PlaceName = i.Place != null ? i.Place.Name : "Невідоме місце",
                StartTime = i.StartTime.ToString(@"hh\:mm\:ss"),
                EndTime = i.EndTime.ToString(@"hh\:mm\:ss"),
                Notes = i.Notes
            });

            string prompt = $@"
                Ти — експертний гід. У місті {destinationCity} почався сильний дощ (Rain Mode).
                Ось поточний розклад подій на день:
                {JsonSerializer.Serialize(originalSchedule)}
                
                Твоє завдання:
                1. Замінити всі вуличні локації (парки, площі, пішохідні зони) на цікаві КРИТІ альтернативи (музеї, галереї, ТРЦ, криті ринки тощо) в цьому ж місті.
                2. Прийоми їжі (ресторани, кафе) залишити без змін та у той же час.
                3. Поверни ТІЛЬКИ чистий масив JSON (без форматування markdown, без ```json на початку). 
                Формат кожного об'єкта має бути строго таким:
                [
                  {{
                    ""DayIndex"": 1,
                    ""PlaceName"": ""Назва закритої локації або ресторану"",
                    ""Category"": ""Attraction"", // Використовуй строго одне з трьох: ""Hotel"", ""Restaurant"", або ""Attraction""
                    ""StartTime"": ""10:00:00"",
                    ""EndTime"": ""12:00:00"",
                    ""Notes"": ""Короткий опис, що тут робити, і чому це ідеально під час дощу""
                  }}
                ]
            ";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            string cleanApiKey = _apiKey.Trim();
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={cleanApiKey}";

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return null; // ЗМІНЕНО

                var responseString = await response.Content.ReadAsStringAsync();

                using var jsonDoc = JsonDocument.Parse(responseString);
                var aiText = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                if (string.IsNullOrEmpty(aiText)) return null; // ЗМІНЕНО

                aiText = aiText.Replace("```json", "").Replace("```", "").Trim();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var generatedItems = JsonSerializer.Deserialize<List<GeminiRouteItem>>(aiText, options);

                if (generatedItems == null || generatedItems.Count == 0) return null; // ЗМІНЕНО

                var newItinerary = new List<ItineraryItem>();

                // Шукаємо CityId
                int cityId = originalItems.FirstOrDefault(i => i.Place != null)?.Place?.CityId 
                             ?? await _context.Cities.Where(c => c.Name == destinationCity).Select(c => c.Id).FirstOrDefaultAsync();

                var cityPlaces = await _context.Places
                    .Where(p => p.CityId == cityId)
                    .ToDictionaryAsync(p => p.Name.ToLowerInvariant(), p => p);

                foreach (var item in generatedItems)
                {
                    var normalizedPlaceName = item.PlaceName.Trim().ToLowerInvariant();

                    if (!cityPlaces.TryGetValue(normalizedPlaceName, out var place))
                    {
                        PlaceType parsedType = PlaceType.Attraction;
                        Enum.TryParse(item.Category, true, out parsedType);

                        place = new Place
                        {
                            CityId = cityId > 0 ? cityId : throw new Exception("CityNotFound"),
                            Name = item.PlaceName,
                            Type = parsedType,
                            Rating = 4.5
                        };

                        _context.Places.Add(place);
                        await _context.SaveChangesAsync();
                        cityPlaces[normalizedPlaceName] = place;
                    }

                    var itineraryItem = new ItineraryItem
                    {
                        PlaceId = place.Id,
                        StartTime = TimeSpan.Parse(item.StartTime),
                        EndTime = TimeSpan.Parse(item.EndTime),
                        Notes = item.Notes
                        // Увага: TripDayId встановлюється вище по ієрархії виклику в TripService
                    };

                    newItinerary.Add(itineraryItem);
                }

                return newItinerary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка режиму Rain Mode: {ex.Message}");
                // ЗМІНЕНО: повертаємо null замість originalItems
                return null; 
            }
        }
    }
}
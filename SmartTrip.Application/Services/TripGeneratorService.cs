using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartTrip.Application.Interfaces;
using SmartTrip.Data;
using SmartTrip.Infrastructure;
using SmartTrip.Models;
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

        public TripGeneratorService(SmartTripDbContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;

            _httpClient.Timeout = TimeSpan.FromMinutes(3);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SmartTripApp/1.0");

            _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new Exception("API ключ не знайдено!");
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

                // 5. Зберігаємо місця та події в БАЗУ ДАНИХ!
                foreach (var item in generatedItems)
                {
                    // Шукаємо, чи ми вже зберігали таке місце раніше
                    var place = await _context.Places
                        .FirstOrDefaultAsync(p => p.Name == item.PlaceName && p.CityId == trip.CityId);

                    if (place == null)
                    {
                        PlaceType parsedType = PlaceType.Attraction; 
                        Enum.TryParse<PlaceType>(item.Category, true, out parsedType);

                        place = new Place
                        {
                            CityId = trip.CityId,
                            Name = item.PlaceName,
                            Type = parsedType, 
                            Rating = 4.5       
                        };
                        _context.Places.Add(place);
                        await _context.SaveChangesAsync(); 
                    }

                    // Знаходимо правильний день для цієї події
                    var tripDay = trip.TripDays.FirstOrDefault(d => d.DayNumber == item.DayIndex);

                    if (tripDay != null)
                    {
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
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Тут можна додати логування, якщо ШІ поверне невалідний JSON
                Console.WriteLine($"Помилка генерації маршруту: {ex.Message}");
            }
        }
    }
}
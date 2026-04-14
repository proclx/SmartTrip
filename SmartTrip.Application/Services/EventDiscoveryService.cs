using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartTrip.Application.Interfaces;
using SmartTrip.Models;

namespace SmartTrip.Application.Services
{
    public class EventDiscoveryService : IEventDiscoveryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public EventDiscoveryService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Ticketmaster:ApiKey"]
                ?? throw new InvalidOperationException("Ticketmaster ApiKey is missing in configuration.");
        }

        public async Task<IEnumerable<LocalEvent>> GetEventsAsync(int cityId, string cityName, DateTime startDate, DateTime endDate)
        {
            var eventsList = new List<LocalEvent>();

            try
            {
                // Ticketmaster очікує дати у форматі ISO 8601 (напр. 2026-04-14T00:00:00Z)
                string startDateTime = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string endDateTime = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // Для тестування англійської використовуйте cityName
                string englishCityName = cityName == "Лондон" ? "London" : 
                                         cityName == "Париж" ? "Paris" : 
                                         cityName == "Нью-Йорк" ? "New York" : cityName;

                // Формуємо URL запиту
                string url = $"discovery/v2/events.json?city={englishCityName}&startDateTime={startDateTime}&endDateTime={endDateTime}&size=10&sort=date,asc&apikey={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return eventsList;

                var content = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(content);
                
                // Дістаємо масив подій
                var eventsArray = jsonNode?["_embedded"]?["events"]?.AsArray();

                if (eventsArray != null)
                {
                    foreach (var evt in eventsArray)
                    {
                        try 
                        {
                            var eventDateStr = evt["dates"]?["start"]?["localDate"]?.ToString();
                            DateTime eventDate = DateTime.Now;
                            if (!string.IsNullOrEmpty(eventDateStr))
                            {
                                DateTime.TryParse(eventDateStr, out eventDate);
                            }

                            // Дістаємо найякісніше зображення або беремо перше
                            string imageUrl = "";
                            var images = evt["images"]?.AsArray();
                            if (images != null && images.Count > 0)
                            {
                                imageUrl = images[0]?["url"]?.ToString() ?? "";
                            }
                            
                            string description = "Цікава подія";
                            var classifications = evt["classifications"]?.AsArray();
                            if (classifications != null && classifications.Count > 0)
                            {
                                description = classifications[0]?["genre"]?["name"]?.ToString() ?? "Подія";
                            }

                            eventsList.Add(new LocalEvent
                            {
                                CityId = cityId,
                                Title = evt["name"]?.ToString() ?? "Невідома подія",
                                Description = description,
                                EventDate = eventDate,
                                LocationUrl = evt["url"]?.ToString() ?? string.Empty,
                                ImageUrl = imageUrl
                            });
                        }
                        catch (Exception exInner)
                        {
                            // Ігноруємо помилки парсингу окремих подій
                            Console.WriteLine($"Помилка парсингу події: {exInner.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TICKETMASTER ERROR: {ex.Message}");
            }

            return eventsList;
        }
    }
}
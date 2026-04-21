using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SmartTrip.Application.Services;
using SmartTrip.Data;
using SmartTrip.Models;
using Xunit;

namespace SmartTrip.Tests.Services
{
    public class TripGeneratorServiceTests
    {
        [Fact]
        public async Task GenerateItineraryAsync_ShouldUseSharedCache_ForSameParameters()
        {
            var context = GetInMemoryDbContext();
            var city = await SeedCityAsync(context, "Kyiv");

            var trip1 = await SeedTripAsync(context, city.Id, "user-1");
            var trip2 = await SeedTripAsync(context, city.Id, "user-1");

            var handler = new StubHttpMessageHandler(CreateGeminiResponse());
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = CreateService(context, handler, memoryCache);

            await service.GenerateItineraryAsync(trip1.Id);
            await service.GenerateItineraryAsync(trip2.Id);

            Assert.Equal(1, handler.CallCount);

            var trip1Items = await context.ItineraryItems.CountAsync(i => i.TripDay!.TripId == trip1.Id);
            var trip2Items = await context.ItineraryItems.CountAsync(i => i.TripDay!.TripId == trip2.Id);
            Assert.True(trip1Items > 0);
            Assert.True(trip2Items > 0);
        }

        [Fact]
        public async Task GenerateItineraryAsync_ShouldUseSharedCache_ForDifferentUsersWithSameParameters()
        {
            var context = GetInMemoryDbContext();
            var city = await SeedCityAsync(context, "Kyiv");

            var trip1 = await SeedTripAsync(context, city.Id, "user-1");
            var trip2 = await SeedTripAsync(context, city.Id, "user-2");

            var handler = new StubHttpMessageHandler(CreateGeminiResponse());
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = CreateService(context, handler, memoryCache);

            await service.GenerateItineraryAsync(trip1.Id);
            await service.GenerateItineraryAsync(trip2.Id);

            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task GenerateItineraryAsync_ShouldNotUseCache_ForDifferentStartingPoints()
        {
            var context = GetInMemoryDbContext();
            var city = await SeedCityAsync(context, "Kyiv");

            var trip1 = await SeedTripAsync(context, city.Id, "user-1", "Lviv");
            var trip2 = await SeedTripAsync(context, city.Id, "user-2", "Odesa");

            var handler = new StubHttpMessageHandler(CreateGeminiResponse());
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = CreateService(context, handler, memoryCache);

            await service.GenerateItineraryAsync(trip1.Id);
            await service.GenerateItineraryAsync(trip2.Id);

            Assert.Equal(2, handler.CallCount);
        }

        private static TripGeneratorService CreateService(SmartTripDbContext context, StubHttpMessageHandler handler, IMemoryCache memoryCache)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleAI:ApiKey"] = "test-api-key",
                ["CacheSettings:TripGenerationCacheMinutes"] = "120"
            }).Build();

            var httpClient = new HttpClient(handler);
            return new TripGeneratorService(context, httpClient, configuration, memoryCache);
        }

        private static async Task<City> SeedCityAsync(SmartTripDbContext context, string cityName)
        {
            var city = new City { Name = cityName, Country = "UA" };
            context.Cities.Add(city);
            await context.SaveChangesAsync();
            return city;
        }

        private static async Task<Trip> SeedTripAsync(SmartTripDbContext context, int cityId, string userId, string? startingPoint = null)
        {
            var trip = new Trip
            {
                CityId = cityId,
                UserId = userId,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                StartingPoint = startingPoint
            };

            context.Trips.Add(trip);
            await context.SaveChangesAsync();

            context.TripDays.Add(new TripDay
            {
                TripId = trip.Id,
                DayNumber = 1,
                Date = DateTime.UtcNow.Date
            });

            await context.SaveChangesAsync();
            return trip;
        }

        private static SmartTripDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SmartTripDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new SmartTripDbContext(options);
        }

        private static string CreateGeminiResponse()
        {
            var generatedArray = "[{\"DayIndex\":1,\"PlaceName\":\"St. Sophia\",\"Category\":\"Attraction\",\"StartTime\":\"10:00:00\",\"EndTime\":\"11:00:00\",\"Notes\":\"Visit\"}]";
            return $"{{\"candidates\":[{{\"content\":{{\"parts\":[{{\"text\":\"{generatedArray}\"}}]}}}}]}}";
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseBody;
            private int _callCount;

            public StubHttpMessageHandler(string responseBody)
            {
                _responseBody = responseBody;
            }

            public int CallCount => _callCount;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _callCount);

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
                };

                return Task.FromResult(response);
            }
        }
    }
}

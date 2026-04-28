using Microsoft.EntityFrameworkCore;
using Moq;
using SmartTrip.Application.Interfaces;
using SmartTrip.Application.Services;
using SmartTrip.Data;
using SmartTrip.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartTrip.Tests.Services
{
    public class TripServiceTests
    {
        private SmartTripDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SmartTripDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new SmartTripDbContext(options);
        }

        private TripService CreateTripService(SmartTripDbContext context)
        {
            var tripGeneratorServiceMock = new Mock<ITripGeneratorService>();
            var packingServiceMock = new Mock<IPackingService>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:ReferenceDataCacheMinutes"] = "60"
            }).Build();

            return new TripService(context, tripGeneratorServiceMock.Object, packingServiceMock.Object, memoryCache, configuration);
        }

        [Fact]
        public async Task UpdateItineraryItemAsync_ShouldUpdateItem_WhenItemExists()
        {
            var context = GetInMemoryDbContext();
            
            var item = new ItineraryItem 
            { 
                Id = 1, 
                Notes = "Стара назва",
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0)
            };
            context.ItineraryItems.Add(item);
            await context.SaveChangesAsync();

            var tripService = CreateTripService(context);

            var result = await tripService.UpdateItineraryItemAsync(
                itemId: 1, 
                newTitle: "Нова назва", 
                newDescription: "Новий опис", 
                newTime: new TimeSpan(12, 0, 0));

            Assert.True(result);
            
            var updatedItem = await context.ItineraryItems.FindAsync(1);
            Assert.NotNull(updatedItem);
            Assert.Equal("Нова назва", updatedItem.Notes);
            Assert.Equal("Новий опис", updatedItem.Notes);
            Assert.Equal(new TimeSpan(12, 0, 0), updatedItem.StartTime);
        }

        [Fact]
        public async Task UpdateItineraryItemAsync_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            var context = GetInMemoryDbContext();
            var tripService = CreateTripService(context);

            var result = await tripService.UpdateItineraryItemAsync(
                itemId: 99, 
                newTitle: "Нова назва", 
                newDescription: "Новий опис", 
                newTime: new TimeSpan(12, 0, 0));

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteItineraryItemAsync_ShouldRemoveItem_WhenItemExists()
        {
            var context = GetInMemoryDbContext();
            var item = new ItineraryItem { Id = 1, Notes = "Локація для видалення" };
            context.ItineraryItems.Add(item);
            await context.SaveChangesAsync();

            var tripService = CreateTripService(context);

            var result = await tripService.DeleteItineraryItemAsync(1);

            Assert.True(result);
            
            var deletedItem = await context.ItineraryItems.FindAsync(1);
            Assert.Null(deletedItem);
        }

        [Fact]
        public async Task DeleteItineraryItemAsync_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            var context = GetInMemoryDbContext();
            var tripService = CreateTripService(context);

            var result = await tripService.DeleteItineraryItemAsync(99);

            Assert.False(result);
        }

        //[Fact]
        //public async Task ApplyRainModeToDayAsync_ShouldReplaceItems_WhenAIGeneratesAlternatives()
        //{
        //    // Arrange
        //    var dbContext = GetInMemoryDbContext();
            
        //    // Mock AI Generator
        //    var mockGenerator = new Mock<ITripGeneratorService>();
        //    mockGenerator
        //        .Setup(g => g.AdaptForRainModeAsync(It.IsAny<string>(), It.IsAny<List<ItineraryItem>>()))
        //        .ReturnsAsync(new List<ItineraryItem> 
        //        { 
        //            new ItineraryItem { Title = "National Art Museum (Indoor)", Time = new TimeSpan(10, 0, 0) } 
        //        });

        //    // Note: Ensure the mock generator is passed into the TripService constructor here
        //    var tripService = new TripService(dbContext, mockGenerator.Object);

        //    var trip = new Trip { Id = 10, UserId = "user123", DestinationName = "Lviv" };
        //    var tripDay = new TripDay { Id = 5, TripId = 10 };
        //    var outdoorItem = new ItineraryItem { Id = 1, TripDayId = 5, Title = "City Center Walking Tour" };

        //    tripDay.ItineraryItems = new List<ItineraryItem> { outdoorItem };
        //    trip.TripDays = new List<TripDay> { tripDay };

        //    dbContext.Trips.Add(trip);
        //    dbContext.TripDays.Add(tripDay);
        //    dbContext.ItineraryItems.Add(outdoorItem);
        //    await dbContext.SaveChangesAsync();

        //    // Act
        //    var result = await tripService.ApplyRainModeToDayAsync(5, "user123");

        //    // Assert
        //    Assert.True(result);
            
        //    // Verify old outdoor item was replaced
        //    var newItems = await dbContext.ItineraryItems.Where(i => i.TripDayId == 5).ToListAsync();
        //    Assert.Single(newItems);
        //    Assert.Contains("Indoor", newItems.First().Title);
        //}
    }
}
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartTrip.Application.Interfaces;
using SmartTrip.Application.Services;
using SmartTrip.Data;
using SmartTrip.Models;
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
            return new TripService(context, tripGeneratorServiceMock.Object, packingServiceMock.Object);
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
    }
}
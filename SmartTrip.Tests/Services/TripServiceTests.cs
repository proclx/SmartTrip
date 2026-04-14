using Microsoft.EntityFrameworkCore;
using Moq;
using SmartTrip.Application.Interfaces;
using SmartTrip.Application.Services;
using SmartTrip.Data;
using SmartTrip.Models;
using System.Threading.Tasks;
using Xunit;

namespace SmartTrip.Tests.Services
{
    public class TripServiceTests
    {
        [Fact]
        public async Task UpdateTripAsync_ShouldUpdateNotes_WhenTripExists()
        {
            // 1. ARRANGE (Підготовка)
            // Налаштовуємо "фейкову" базу даних у пам'яті
            var options = new DbContextOptionsBuilder<SmartTripDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_UpdateNotes")
                .Options;

            using (var context = new SmartTripDbContext(options))
            {
                // Додаємо тестову подорож у пам'ять
                context.Trips.Add(new Trip
                {
                    Id = 1,
                    UserId = "user-123",
                    CityId = 1,
                    PeopleCount = 1
                });
                await context.SaveChangesAsync();
            }

            // Створюємо "заглушки" (Mocks) для інших сервісів, які вимагає конструктор
            var mockGeneratorService = new Mock<ITripGeneratorService>();
            var mockPackingService = new Mock<IPackingService>();

            using (var context = new SmartTripDbContext(options))
            {
                var tripService = new TripService(context, mockGeneratorService.Object, mockPackingService.Object);

                // 2. ACT (Виконання)
                // Намагаємося оновити подорож: ставимо 3 людини, рейтинг 5 і нові нотатки
                var result = await tripService.UpdateTripAsync(
                    tripId: 1,
                    userId: "user-123",
                    peopleCount: 3,
                    rating: 5,
                    notes: "Мій супер секретний пароль: 1234");

                // 3. ASSERT (Перевірка)
                Assert.True(result); // Метод має повернути true

                // Дістаємо подорож з бази і перевіряємо, чи змінилися дані
                var updatedTrip = await context.Trips.FindAsync(1);
                Assert.NotNull(updatedTrip);
                Assert.Equal("Мій супер секретний пароль: 1234", updatedTrip.Notes);
                Assert.Equal(3, updatedTrip.PeopleCount);
                Assert.Equal(5, updatedTrip.Rating);
            }
        }

        [Fact]
        public async Task UpdateTripAsync_ShouldReturnFalse_WhenTripDoesNotExist()
        {
            // 1. ARRANGE
            var options = new DbContextOptionsBuilder<SmartTripDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_TripNotFound")
                .Options;

            var mockGeneratorService = new Mock<ITripGeneratorService>();
            var mockPackingService = new Mock<IPackingService>();

            using (var context = new SmartTripDbContext(options))
            {
                var tripService = new TripService(context, mockGeneratorService.Object, mockPackingService.Object);

                // 2. ACT (Пробуємо оновити подорож з неіснуючим ID = 99)
                var result = await tripService.UpdateTripAsync(99, "user-123", 2, null, "Нотатка");

                // 3. ASSERT
                Assert.False(result); // Має повернути false, бо подорожі немає
            }
        }
    }
}
using Microsoft.EntityFrameworkCore;
using SmartTrip.Application.Services;
using SmartTrip.Data;
using SmartTrip.Models;
using Xunit;

namespace SmartTrip.Tests.Services
{
    public class PackingServiceTests
    {
        private readonly SmartTripDbContext _dbContext;
        private readonly PackingService _packingService;

        public PackingServiceTests()
        {
            var options = new DbContextOptionsBuilder<SmartTripDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new SmartTripDbContext(options);
            _packingService = new PackingService(_dbContext);
        }

        [Fact]
        public async Task AddDefaultItemAsync_ShouldAddItem_WithValidData()
        {
            // Arrange
            var userId = "user-123";
            var itemName = "Паспорт";
            var category = "Документи та фінанси";

            // Act
            var result = await _packingService.AddDefaultItemAsync(userId, itemName, category);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemName, result.Name);
            Assert.Equal(category, result.Category);
            Assert.Equal(userId, result.UserId);

            var savedItem = await _dbContext.DefaultPackingItems.FirstOrDefaultAsync(x => x.Id == result.Id);
            Assert.NotNull(savedItem);
            Assert.Equal(itemName, savedItem.Name);
        }

        [Fact]
        public async Task UpdateDefaultItemAsync_ShouldUpdateItem_WithValidData()
        {
            // Arrange
            var userId = "user-123";
            var item = new DefaultPackingItem
            {
                UserId = userId,
                Name = "Паспорт",
                Category = "Документи та фінанси"
            };

            _dbContext.DefaultPackingItems.Add(item);
            await _dbContext.SaveChangesAsync();

            var newName = "Закордонний паспорт";
            var newCategory = "Документи та фінанси";

            // Act
            await _packingService.UpdateDefaultItemAsync(item.Id, newName, newCategory, userId);

            // Assert
            var updatedItem = await _dbContext.DefaultPackingItems.FirstOrDefaultAsync(x => x.Id == item.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal(newName, updatedItem.Name);
            Assert.Equal(newCategory, updatedItem.Category);
        }

        [Fact]
        public async Task UpdateDefaultItemAsync_ShouldChangeCategory_WhenNewCategoryProvided()
        {
            // Arrange
            var userId = "user-123";
            var item = new DefaultPackingItem
            {
                UserId = userId,
                Name = "Рюкзак",
                Category = "Документи та фінанси"
            };

            _dbContext.DefaultPackingItems.Add(item);
            await _dbContext.SaveChangesAsync();

            var newCategory = "Техніка";

            // Act
            await _packingService.UpdateDefaultItemAsync(item.Id, "Рюкзак", newCategory, userId);

            // Assert
            var updatedItem = await _dbContext.DefaultPackingItems.FirstOrDefaultAsync(x => x.Id == item.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal(newCategory, updatedItem.Category);
        }

        [Fact]
        public async Task UpdateDefaultItemAsync_ShouldNotUpdateOtherUsersItem_WhenUserIdMismatch()
        {
            // Arrange
            var userId1 = "user-123";
            var userId2 = "user-456";
            var item = new DefaultPackingItem
            {
                UserId = userId1,
                Name = "Паспорт",
                Category = "Документи та фінанси"
            };

            _dbContext.DefaultPackingItems.Add(item);
            await _dbContext.SaveChangesAsync();

            // Act
            await _packingService.UpdateDefaultItemAsync(item.Id, "Новий паспорт", "Документи та фінанси", userId2);

            // Assert
            var unchangedItem = await _dbContext.DefaultPackingItems.FirstOrDefaultAsync(x => x.Id == item.Id);
            Assert.NotNull(unchangedItem);
            Assert.Equal("Паспорт", unchangedItem.Name); // Назва не змінилась
        }

        [Fact]
        public async Task UpdateDefaultItemAsync_ShouldSetDefaultCategory_WhenCategoryIsNull()
        {
            // Arrange
            var userId = "user-123";
            var item = new DefaultPackingItem
            {
                UserId = userId,
                Name = "Щось",
                Category = "Документи та фінанси"
            };

            _dbContext.DefaultPackingItems.Add(item);
            await _dbContext.SaveChangesAsync();

            // Act
            await _packingService.UpdateDefaultItemAsync(item.Id, "Щось", null, userId);

            // Assert
            var updatedItem = await _dbContext.DefaultPackingItems.FirstOrDefaultAsync(x => x.Id == item.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal("Інше", updatedItem.Category);
        }

        [Fact]
        public async Task DeleteDefaultItemAsync_ShouldDeleteItem_WhenValid()
        {
            // Arrange
            var userId = "user-123";
            var item = new DefaultPackingItem
            {
                UserId = userId,
                Name = "Паспорт",
                Category = "Документи та фінанси"
            };

            _dbContext.DefaultPackingItems.Add(item);
            await _dbContext.SaveChangesAsync();

            // Act
            await _packingService.DeleteDefaultItemAsync(item.Id, userId);

            // Assert
            var deletedItem = await _dbContext.DefaultPackingItems.FirstOrDefaultAsync(x => x.Id == item.Id);
            Assert.Null(deletedItem);
        }

        [Fact]
        public async Task GetDefaultItemsAsync_ShouldReturnOnlyUserItems()
        {
            // Arrange
            var userId1 = "user-123";
            var userId2 = "user-456";

            var item1 = new DefaultPackingItem { UserId = userId1, Name = "Паспорт", Category = "Документи та фінанси" };
            var item2 = new DefaultPackingItem { UserId = userId1, Name = "Квитки", Category = "Документи та фінанси" };
            var item3 = new DefaultPackingItem { UserId = userId2, Name = "Чемодан", Category = "Інше" };

            _dbContext.DefaultPackingItems.AddRange(item1, item2, item3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _packingService.GetDefaultItemsAsync(userId1);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, item => Assert.Equal(userId1, item.UserId));
        }
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using SmartTrip.Application.Services;
using SmartTrip.Models;
using Xunit;

namespace SmartTrip.Tests.AccountTests
{
    public class RegisterTests
    {
        private Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task RegisterAsync_WhenSuccessful_ShouldReturnSuccess()
        { 
            var userManagerMock = MockUserManager();

            // Налаштовуємо мок: коли викликається CreateAsync, повертаємо успіх
            userManagerMock
                .Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var authService = new AuthService(userManagerMock.Object);

            var result = await authService.RegisterAsync("test@gmail.com", "Password123!", "Тарас", "Шевченко");

            Assert.True(result.Succeeded); // Перевіряємо, що результат успішний

            // Перевіряємо, що метод CreateAsync у UserManager був викликаний рівно 1 раз
            userManagerMock.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenFailed_ShouldReturnErrors()
        {
            var userManagerMock = MockUserManager();
            var expectedError = new IdentityError { Description = "Email already exists" };

            // Налаштовуємо мок: коли викликається CreateAsync, імітуємо помилку БД
            userManagerMock
                .Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(expectedError));

            var authService = new AuthService(userManagerMock.Object);

            var result = await authService.RegisterAsync("test@gmail.com", "Password123!", "Тарас", "Шевченко");

            Assert.False(result.Succeeded); // Перевіряємо, що результат неуспішний

            // Перевіряємо, що повернулася правильна помилка
            Assert.Contains(result.Errors, e => e.Description == "Email already exists");
        }
    }
    
}

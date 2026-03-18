using Microsoft.AspNetCore.Identity;
using Moq;
using SmartTrip.Application.Services;
using SmartTrip.Models;
using Xunit;

namespace SmartTrip.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Identity UserManager складно ініціалізувати вручну, тому використовуємо Moq
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // Створюємо екземпляр сервісу. SignInManager нам тут не потрібен, передаємо null
            _authService = new AuthService(_userManagerMock.Object, null!);
        }

        [Fact]
        public async Task GeneratePasswordResetTokenAsync_ShouldReturnToken_WhenUserExists()
        {
            // Arrange (Підготовка)
            var email = "test@smarttrip.com";
            var user = new User { Email = email };
            var expectedToken = "secure-reset-token-123";

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                            .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                            .ReturnsAsync(expectedToken);

            // Act (Дія)
            var result = await _authService.GeneratePasswordResetTokenAsync(email);

            // Assert (Перевірка результату)
            Assert.NotNull(result);
            Assert.Equal(expectedToken, result);
        }

        [Fact]
        public async Task GeneratePasswordResetTokenAsync_ShouldReturnNull_WhenUserNotFound()
        {
            // Arrange
            var email = "nonexistent@test.com";
            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                            .ReturnsAsync((User)null!);

            // Act
            var result = await _authService.GeneratePasswordResetTokenAsync(email);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnSuccess_WhenDataIsValid()
        {
            // Arrange
            var email = "user@test.com";
            var token = "valid-token";
            var newPassword = "NewPassword123!";
            var user = new User { Email = email };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                            .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, token, newPassword))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.ResetPasswordAsync(email, token, newPassword);

            // Assert
            Assert.True(result.Succeeded);
        }
    }
}
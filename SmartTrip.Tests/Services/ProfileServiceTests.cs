using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartTrip.Application.Services;
using SmartTrip.Data;
using SmartTrip.Models;
using Xunit;

namespace SmartTrip.Tests.Services
{
    public class ProfileServiceTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly Mock<SmartTripDbContext> _dbContextMock;
        private readonly ProfileService _profileService;

        public ProfileServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            _environmentMock = new Mock<IWebHostEnvironment>();

            var options = new DbContextOptionsBuilder<SmartTripDbContext>().Options;
            _dbContextMock = new Mock<SmartTripDbContext>(options);

            _profileService = new ProfileService(_dbContextMock.Object, _environmentMock.Object, _userManagerMock.Object);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldReturnSuccess_WhenCurrentPasswordIsCorrect()
        {
            // Arrange
            var userId = "user-123";
            var currentPassword = "OldPassword123!";
            var newPassword = "NewPassword123!";
            var user = new User { Id = userId, Email = "test@smarttrip.com" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId))
                            .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _profileService.ChangePasswordAsync(userId, currentPassword, newPassword);

            // Assert
            Assert.True(result.Succeeded);
            _userManagerMock.Verify(x => x.ChangePasswordAsync(user, currentPassword, newPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldReturnFailed_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = "missing-user";
            var currentPassword = "OldPassword123!";
            var newPassword = "NewPassword123!";

            _userManagerMock.Setup(x => x.FindByIdAsync(userId))
                            .ReturnsAsync((User)null!);

            // Act
            var result = await _profileService.ChangePasswordAsync(userId, currentPassword, newPassword);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description.Contains("Користувача не знайдено"));
        }
    }
}

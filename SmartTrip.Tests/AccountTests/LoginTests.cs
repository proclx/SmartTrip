using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartTrip.Application.Services;
using SmartTrip.Models;
using Xunit;

namespace SmartTrip.Tests.AccountTests
{
    public class LoginTests
    {
        private Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<SignInManager<User>> MockSignInManager(UserManager<User> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            return new Mock<SignInManager<User>>(userManager, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        [Fact]
        public async Task LoginAsync_WhenSuccessful_ShouldReturnSuccess()
        {
            var userManagerMock = MockUserManager();
            var signInManagerMock = MockSignInManager(userManagerMock.Object);

            signInManagerMock
                .Setup(s => s.PasswordSignInAsync("test@example.com", "Password123", true, false))
                .ReturnsAsync(SignInResult.Success);

            var authService = new AuthService(userManagerMock.Object, signInManagerMock.Object);
            
            var result = await authService.LoginAsync("test@example.com", "Password123", true);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task LoginAsync_WhenFailed_ShouldReturnFailed()
        {
            var userManagerMock = MockUserManager();
            var signInManagerMock = MockSignInManager(userManagerMock.Object);

            signInManagerMock
                .Setup(s => s.PasswordSignInAsync("test@example.com", "WrongPassword", false, false))
                .ReturnsAsync(SignInResult.Failed);

            var authService = new AuthService(userManagerMock.Object, signInManagerMock.Object);
            
            var result = await authService.LoginAsync("test@example.com", "WrongPassword", false);

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task LogoutAsync_ShouldCallSignOut()
        {
            var userManagerMock = MockUserManager();
            var signInManagerMock = MockSignInManager(userManagerMock.Object);

            signInManagerMock.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

            var authService = new AuthService(userManagerMock.Object, signInManagerMock.Object);
            
            await authService.LogoutAsync();

            signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once);
        }
    }
}
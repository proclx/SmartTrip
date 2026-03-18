using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Додано для IEmailSender
using Moq;
using SmartTrip.Application.Interfaces;
using SmartTrip.UI.Controllers;
using SmartTrip.UI.ViewModels;
using Xunit;

namespace SmartTrip.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<IEmailSender> _emailSenderMock;

        public AccountControllerTests()
        {
            _emailSenderMock = new Mock<IEmailSender>();
        }

        [Fact]
        public void Login_Get_ReturnsViewResult()
        {
            var authServiceMock = new Mock<IAuthService>();
            var controller = new AccountController(authServiceMock.Object, _emailSenderMock.Object);

            var result = controller.Login();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Login_Post_InvalidModel_ReturnsViewWithModel()
        {
            var authServiceMock = new Mock<IAuthService>();
            var controller = new AccountController(authServiceMock.Object, _emailSenderMock.Object);
            controller.ModelState.AddModelError("Email", "Required");
            var model = new LoginViewModel();

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_ValidCreds_RedirectsToHomeIndex()
        {
            var authServiceMock = new Mock<IAuthService>();
            authServiceMock
                .Setup(s => s.LoginAsync("test@example.com", "Pass", true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var controller = new AccountController(authServiceMock.Object, _emailSenderMock.Object);
            var model = new LoginViewModel { Email = "test@example.com", Password = "Pass", RememberMe = true };

            var result = await controller.Login(model);

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_InvalidCreds_ReturnsViewWithError()
        {
            var authServiceMock = new Mock<IAuthService>();
            authServiceMock
                .Setup(s => s.LoginAsync("test@example.com", "Pass", true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var controller = new AccountController(authServiceMock.Object, _emailSenderMock.Object);
            var model = new LoginViewModel { Email = "test@example.com", Password = "Pass", RememberMe = true };

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
        }

        [Fact]
        public async Task Logout_Post_CallsLogoutAndRedirects()
        {
            var authServiceMock = new Mock<IAuthService>();
            authServiceMock.Setup(s => s.LogoutAsync()).Returns(Task.CompletedTask);
            var controller = new AccountController(authServiceMock.Object, _emailSenderMock.Object);

            var result = await controller.Logout();

            authServiceMock.Verify(s => s.LogoutAsync(), Times.Once);
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }
    }
}
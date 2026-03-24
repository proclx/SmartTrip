using Microsoft.AspNetCore.Mvc;
using SmartTrip.Application.Interfaces;
using SmartTrip.UI.ViewModels;
using Serilog;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace SmartTrip.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService authService;
        private readonly IEmailSender emailSender;

        public AccountController(IAuthService authService, IEmailSender emailSender)
        {
            this.authService = authService;
            this.emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await authService.RegisterAsync(model.Email, model.Password, model.FirstName, model.LastName);
            if (result.Succeeded)
            {
                Log.Information("User {Email} registered successfully", model.Email);
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await authService.LoginAsync(model.Email, model.Password, model.RememberMe);
            if (result.Succeeded)
            {
                Log.Information("User {Email} logged in", model.Email);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await authService.LogoutAsync();
            Log.Information("User logged out");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = await authService.GeneratePasswordResetTokenAsync(model.Email);

            if (token != null)
            {
                var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { token, email = model.Email }, Request.Scheme);

                Log.Information("Password reset link for {Email}: {Link}", model.Email, callbackUrl);

                var message = $"Будь ласка, скиньте ваш пароль, натиснувши тут: <a href='{callbackUrl}'>посилання</a>";
                await emailSender.SendEmailAsync(model.Email, "Скидання пароля - SmartTrip", message);
            }

            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token = null, string email = null)
        {
            if (token == null || email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await authService.ResetPasswordAsync(model.Email, model.Token, model.Password);

            if (result.Succeeded)
            {
                Log.Information("Password reset successfully for {Email}", model.Email);
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
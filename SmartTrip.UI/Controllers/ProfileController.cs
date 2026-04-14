using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartTrip.Application.Interfaces;
using SmartTrip.Models;
using SmartTrip.UI.ViewModels;
using System.Security.Claims;

namespace SmartTrip.UI.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly SignInManager<User> _signInManager;
        private readonly IPackingService _packingService; // 1. Додаємо сервіс чеклистів

        public ProfileController(IProfileService profileService, SignInManager<User> signInManager, IPackingService packingService)
        {
            _profileService = profileService;
            _signInManager = signInManager;
            _packingService = packingService; // 2. Ініціалізуємо сервіс
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _profileService.GetUserProfileAsync(userId!);

            if (user == null)
                return NotFound();

            // 3. Отримуємо базовий чеклист користувача
            var defaultItems = await _packingService.GetDefaultItemsAsync(userId!);

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                DefaultPackingItems = defaultItems // 4. Передаємо у модель
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _profileService.GetUserProfileAsync(userId!);

            if (user == null)
                return NotFound();

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var success = await _profileService.UpdateUserProfileAsync(userId!, model.FirstName!, model.LastName!, model.Email!);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Помилка при оновленні профіля.");
                return View(model);
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var imageSuccess = await _profileService.UploadProfileImageAsync(userId!, model.ImageFile);
                if (!imageSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Помилка при завантаженні зображення. Переконайтеся, що файл - це зображення (JPEG, PNG, GIF) розміром менше 5MB.");
                    return View(model);
                }
            }

            var updatedUser = await _profileService.GetUserProfileAsync(userId!);
            if (updatedUser != null)
            {
                await _signInManager.RefreshSignInAsync(updatedUser);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _profileService.ChangePasswordAsync(userId!, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            var updatedUser = await _profileService.GetUserProfileAsync(userId!);
            if (updatedUser != null)
            {
                await _signInManager.RefreshSignInAsync(updatedUser);
            }

            TempData["SuccessMessage"] = "Пароль було успішно змінено.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _profileService.DeleteProfileImageAsync(userId!);

            if (!success)
            {
                return BadRequest("Помилка при видаленні зображення.");
            }

            return RedirectToAction("Index");
        }

        // --- МЕТОДИ ДЛЯ БАЗОВОГО ЧЕКЛИСТУ ---

        [HttpPost]
        public async Task<IActionResult> AddDefaultItem(string name, string category)
        {
            if (string.IsNullOrWhiteSpace(name))
                return RedirectToAction("Index");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _packingService.AddDefaultItemAsync(userId!, name, category);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDefaultItem(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _packingService.DeleteDefaultItemAsync(itemId, userId!);

            return RedirectToAction("Index");
        }
    }
}
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

        public ProfileController(IProfileService profileService, SignInManager<User> signInManager)
        {
            _profileService = profileService;
            _signInManager = signInManager;
        }

        // Відображення профіля користувача
        [HttpGet]
        public async Task<IActionResult> Index()
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

        // Сторінка редагування профіля
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

        // Оновлення даних профіля
        [HttpPost]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Оновлюємо текстові дані
            var success = await _profileService.UpdateUserProfileAsync(userId!, model.FirstName!, model.LastName!, model.Email!);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Помилка при оновленні профіля.");
                return View(model);
            }

            // Якщо користувач завантажив нове фото
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var imageSuccess = await _profileService.UploadProfileImageAsync(userId!, model.ImageFile);
                if (!imageSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Помилка при завантаженні зображення. Переконайтеся, що файл - це зображення (JPEG, PNG, GIF) розміром менше 5MB.");
                    return View(model);
                }
            }

            // Оновлюємо сесію користувача, якщо змінився Email або інша інформація
            var updatedUser = await _profileService.GetUserProfileAsync(userId!);
            if (updatedUser != null)
            {
                await _signInManager.RefreshSignInAsync(updatedUser);
            }

            return RedirectToAction("Index");
        }

        // Видалення фото профіля
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
    }
}

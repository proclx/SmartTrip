using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartTrip.Application.Interfaces;
using SmartTrip.UI.ViewModels;
using System.Security.Claims;

namespace SmartTrip.UI.Controllers
{
    [Authorize] // Галерея доступна тільки авторизованим користувачам
    public class GalleryController : Controller
    {
        private readonly IGalleryService _galleryService;
        private readonly ITripService _tripService;

        public GalleryController(IGalleryService galleryService, ITripService tripService)
        {
            _galleryService = galleryService;
            _tripService = tripService;
        }

        // Відображення всіх фото користувача
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var photos = await _galleryService.GetUserPhotosAsync(userId!);
            return View(photos);
        }

        // Сторінка завантаження фото
        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Отримуємо подорожі та перетворюємо їх у SelectList з назвою та датами
            var trips = await _tripService.GetUserTripsAsync(userId!);

            var model = new UploadPhotoViewModel
            {
                Trips = trips.Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.City?.Name} ({t.StartDate.ToShortDateString()} - {t.EndDate.ToShortDateString()})"
                })
            };

            return View(model);
        }

        // Обробка завантаження
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadPhotoViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                await _galleryService.UploadPhotosAsync(model.Files, model.SelectedTripId, userId!);
                return RedirectToAction(nameof(Index));
            }

            // Якщо модель невалідна, переповнюємо список подорожей знову
            var trips = await _tripService.GetUserTripsAsync(userId!);
            model.Trips = trips.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"{t.City?.Name} ({t.StartDate.ToShortDateString()} - {t.EndDate.ToShortDateString()})"
            });

            return View(model);
        }

        // Видалення фото
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _galleryService.DeletePhotoAsync(id, userId!);
            return RedirectToAction(nameof(Index));
        }
    }
}
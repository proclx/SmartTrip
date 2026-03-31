using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartTrip.Application.Interfaces;
using SmartTrip.Application.Services;
using SmartTrip.Models;
using SmartTrip.UI.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SmartTrip.UI.Controllers
{
    [Authorize]
    public class TripController : Controller
    {
        private readonly ITripService _tripService;
        private readonly IGalleryService _galleryService;
        private readonly UserManager<User> _userManager;
        private readonly IPackingService _packingService; 

        public TripController(ITripService tripService, IGalleryService galleryService, UserManager<User> userManager, IPackingService packingService)
        {
            _tripService = tripService;
            _galleryService = galleryService;
            _userManager = userManager;
            _packingService = packingService; 
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateTripViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var newTripId = await _tripService.CreateTripAsync(userId!, model.DestinationName, model.StartDate, model.EndDate);

            return RedirectToAction("Itinerary", new { id = newTripId });
        }



        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var trips = await _tripService.GetUserTripsAsync(userId!);

            var model = trips.Select(t => new TripViewModel
            {
                Id = t.Id,
                City = t.City?.Name ?? "-",
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                PeopleCount = t.PeopleCount,
                Rating = t.Rating,
                Photos = t.Photos?.Select(p => new TripPhotoViewModel { Id = p.Id, FilePath = p.FilePath }).ToList(),
                IsFavorite = t.IsFavorite
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var trip = await _tripService.GetTripByIdAsync(id, userId!);
            if (trip == null) return NotFound();

            var model = new TripViewModel
            {
                Id = trip.Id,
                City = trip.City?.Name ?? "-",
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                PeopleCount = trip.PeopleCount,
                Rating = trip.Rating,
                IsFavorite = trip.IsFavorite,
                Photos = trip.Photos?.Select(p => new TripPhotoViewModel { Id = p.Id, FilePath = p.FilePath }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Itinerary(int id)
        {
            var trip = await _tripService.GetTripDetailsAsync(id);
            if (trip == null) return NotFound();

            var model = new ItineraryViewModel
            {
                TripId = trip.Id,
                CityName = trip.City?.Name ?? "Невідоме місто",
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Days = trip.TripDays.Select(d => new ItineraryDayViewModel
                {
                    DayIndex = d.DayNumber,
                    Date = d.Date,
                    Items = d.ItineraryItems.Select(i => new ItineraryItemViewModel
                    {
                        PlaceName = i.Place?.Name ?? "Невідоме місце",
                        PlaceType = i.Place?.Type.ToString() ?? "",
                        Rating = i.Place?.Rating,
                        StartTime = i.StartTime,
                        EndTime = i.EndTime,
                        Notes = i.Notes ?? string.Empty
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var trip = await _tripService.GetTripByIdAsync(id, userId!);
            if (trip == null) return NotFound();

            var model = new EditTripViewModel
            {
                Id = trip.Id,
                PeopleCount = trip.PeopleCount,
                Rating = trip.Rating
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTripViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);
            var success = await _tripService.UpdateTripAsync(model.Id, userId!, model.PeopleCount, model.Rating);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Помилка оновлення подорожі");
                return View(model);
            }

            return RedirectToAction("Details", new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhotos(int tripId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["PhotoMessage"] = "Оберіть хоча б одне фото для завантаження.";
                return RedirectToAction("Details", new { id = tripId });
            }

            var userId = _userManager.GetUserId(User);
            await _galleryService.UploadPhotosAsync(files, tripId, userId!);

            return RedirectToAction("Details", new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int tripId, int photoId)
        {
            var userId = _userManager.GetUserId(User);
            await _galleryService.DeletePhotoAsync(photoId, userId!);
            return RedirectToAction("Details", new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var userId = _userManager.GetUserId(User);
            await _tripService.ToggleFavoriteAsync(id, userId!);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = _userManager.GetUserId(User);
            var trips = await _tripService.GetFavoriteTripsAsync(userId!);

            var model = trips.Select(t => new TripViewModel
            {
                Id = t.Id,
                City = t.City?.Name ?? "-",
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                PeopleCount = t.PeopleCount,
                Rating = t.Rating,
                IsFavorite = t.IsFavorite,
                Photos = t.Photos?.Select(p => new TripPhotoViewModel { Id = p.Id, FilePath = p.FilePath }).ToList()
            }).ToList();

            return View(model);
        }

        // --- МЕТОДИ ДЛЯ ЧЕКЛИСТУ ---

        // Завантажує вміст чеклисту для модального вікна
        [HttpGet]
        public async Task<IActionResult> GetPackingListModal(int tripId)
        {
            var userId = _userManager.GetUserId(User);
            var items = await _packingService.GetTripItemsAsync(tripId, userId!);

            // Передаємо TripId у ViewData, щоб знати, куди додавати нові речі
            ViewData["TripId"] = tripId;

            // Повертаємо PartialView (ми її зараз створимо)
            return PartialView("_PackingListPartial", items);
        }

        [HttpPost]
        public async Task<IActionResult> TogglePackingItem(int itemId)
        {
            var userId = _userManager.GetUserId(User);
            await _packingService.ToggleItemStatusAsync(itemId, userId!);
            return Ok(); // Повертаємо 200 OK для AJAX
        }

        [HttpPost]
        public async Task<IActionResult> AddTripPackingItem(int tripId, string name, string category)
        {
            var userId = _userManager.GetUserId(User);
            await _packingService.AddTripItemAsync(tripId, name, category, userId!);

            // Повертаємо оновлений список
            return RedirectToAction(nameof(GetPackingListModal), new { tripId = tripId });
        }

        [HttpPost]
        public async Task<IActionResult> SyncPackingList(int tripId)
        {
            var userId = _userManager.GetUserId(User);
            await _packingService.SyncWithDefaultListAsync(tripId, userId!);
            return RedirectToAction(nameof(GetPackingListModal), new { tripId = tripId });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPackingList(int tripId)
        {
            var userId = _userManager.GetUserId(User);
            await _packingService.ResetToDefaultListAsync(tripId, userId!);
            return RedirectToAction(nameof(GetPackingListModal), new { tripId = tripId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTripPackingItem(int itemId, int tripId)
        {
            var userId = _userManager.GetUserId(User);
            await _packingService.DeleteTripItemAsync(itemId, userId!);

            // Після видалення повертаємо оновлений HTML модалки
            return RedirectToAction(nameof(GetPackingListModal), new { tripId = tripId });
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using SmartTrip.Application.Interfaces;
using SmartTrip.Application.Services;
using SmartTrip.Models;
using SmartTrip.UI.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartTrip.UI.Controllers
{
    [Authorize]
    public class TripController : Controller
    {
        private readonly ITripService _tripService;
        private readonly IGalleryService _galleryService;
        private readonly UserManager<User> _userManager;
        private readonly IPackingService _packingService;
        private readonly IEventDiscoveryService _eventDiscoveryService;

        public TripController(ITripService tripService, IGalleryService galleryService, UserManager<User> userManager, IPackingService packingService, IEventDiscoveryService eventDiscoveryService)
        {
            _tripService = tripService;
            _galleryService = galleryService;
            _userManager = userManager;
            _packingService = packingService;
            _eventDiscoveryService = eventDiscoveryService;
        }

        [HttpGet]
        public IActionResult Create(string? destinationName, string? notes)
        {
            return View(new CreateTripViewModel
            {
                DestinationName = destinationName ?? string.Empty,
                Notes = notes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(CreateTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var newTripId = await _tripService.CreateTripAsync(userId, model.DestinationName, model.StartingPoint, model.StartDate, model.EndDate, model.Notes);

            return RedirectToAction("Itinerary", new { id = newTripId });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var trips = await _tripService.GetUserTripsAsync(userId);

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
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var trip = await _tripService.GetTripByIdAsync(id, userId);
            if (trip == null) return NotFound();

            // Отримуємо події
            var suggestedEvents = await _eventDiscoveryService.GetEventsAsync(
                trip.CityId,
                trip.City.Name,
                trip.StartDate,
                trip.EndDate);

            var model = new TripViewModel
            {
                Id = trip.Id,
                City = trip.City?.Name ?? "-",
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                PeopleCount = trip.PeopleCount,
                Rating = trip.Rating,
                Notes = trip.Notes, // ДОДАНО: передаємо нотатки у View
                IsFavorite = trip.IsFavorite,
                Photos = trip.Photos?.Select(p => new TripPhotoViewModel { Id = p.Id, FilePath = p.FilePath }).ToList(),
                SuggestedEvents = suggestedEvents
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
                StartingPoint = trip.StartingPoint,
                RouteToDestination = trip.RouteToDestination,
                RouteBack = trip.RouteBack,
                Days = trip.TripDays.Select(d => new ItineraryDayViewModel
                {
                    DayIndex = d.DayNumber,
                    Date = d.Date,
                    Items = d.ItineraryItems.Select(i => new ItineraryItemViewModel
                    {
                        Id = i.Id, 
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
        public async Task<IActionResult> ExportPdf(int id)
        {
            var trip = await _tripService.GetTripDetailsAsync(id);
            if (trip == null) return NotFound();

            var model = new ItineraryViewModel
            {
                TripId = trip.Id,
                CityName = trip.City?.Name ?? "Невідоме місто",
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                StartingPoint = trip.StartingPoint,
                RouteToDestination = trip.RouteToDestination,
                RouteBack = trip.RouteBack,
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

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.Header().AlignCenter().Text($"Подорож до {model.CityName}").FontSize(20).Bold();
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Routes
                        if (!string.IsNullOrEmpty(model.RouteToDestination))
                        {
                            column.Item().Text("🚗 Маршрут туди:").Bold().FontSize(16);
                            column.Item().Text(model.RouteToDestination).FontSize(12);
                        }
                        if (!string.IsNullOrEmpty(model.RouteBack))
                        {
                            column.Item().Text("🚗 Маршрут назад:").Bold().FontSize(16);
                            column.Item().Text(model.RouteBack).FontSize(12);
                        }

                        column.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Days
                        foreach (var day in model.Days)
                        {
                            column.Item().Text($"День {day.DayIndex} - {day.Date:dd.MM.yyyy}").Bold().FontSize(14);
                            foreach (var item in day.Items)
                            {
                                column.Item().Text($"{item.StartTime:hh\\:mm} - {item.EndTime:hh\\:mm}: {item.PlaceName} ({item.PlaceType})").FontSize(12);
                                if (!string.IsNullOrEmpty(item.Notes))
                                {
                                    column.Item().Text(item.Notes).FontSize(10).Italic();
                                }
                                column.Item().PaddingBottom(5);
                            }
                            column.Item().PaddingBottom(10);
                        }
                    });
                    page.Footer().AlignCenter().Text("Згенеровано SmartTrip").FontSize(10);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", $"Подорож_{model.CityName}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var trip = await _tripService.GetTripByIdAsync(id, userId);
            if (trip == null) return NotFound();

            var model = new EditTripViewModel
            {
                Id = trip.Id,
                PeopleCount = trip.PeopleCount,
                Rating = trip.Rating,
                Notes = trip.Notes // ДОДАНО: передаємо нотатки для редагування
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
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // ДОДАНО: передаємо model.Notes у сервіс
            var success = await _tripService.UpdateTripAsync(model.Id, userId, model.PeopleCount, model.Rating, model.Notes);

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
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            await _galleryService.UploadPhotosAsync(files, tripId, userId);

            return RedirectToAction("Details", new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int tripId, int photoId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            await _galleryService.DeletePhotoAsync(photoId, userId);
            return RedirectToAction("Details", new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            await _tripService.ToggleFavoriteAsync(id, userId);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var trips = await _tripService.GetFavoriteTripsAsync(userId);

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

        [HttpGet]
        public async Task<IActionResult> Archived()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var trips = await _tripService.GetArchivedTripsAsync(userId);
            var model = trips.Select(t => new TripViewModel
            {
                Id = t.Id,
                City = t.City?.Name ?? "-",
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                PeopleCount = t.PeopleCount,
                Rating = t.Rating,
                IsFavorite = t.IsFavorite,
                IsArchived = t.IsArchived, // Передаємо статус
                Photos = t.Photos?.Select(p => new TripPhotoViewModel { Id = p.Id, FilePath = p.FilePath }).ToList()
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleArchive(int id, string returnUrl)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _tripService.ToggleArchiveAsync(id, userId);

            // Повертаємо користувача на ту сторінку, звідки він натиснув кнопку
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }

        // --- МЕТОДИ ДЛЯ ЧЕКЛИСТУ ---

        [HttpGet]
        public async Task<IActionResult> GetPackingListModal(int tripId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var items = await _packingService.GetTripItemsAsync(tripId, userId);
            ViewData["TripId"] = tripId;

            return PartialView("_PackingListPartial", items);
        }

        [HttpPost]
        public async Task<IActionResult> TogglePackingItem(int itemId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _packingService.ToggleItemStatusAsync(itemId, userId);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddTripPackingItem(int tripId, string name, string category)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _packingService.AddTripItemAsync(tripId, name, category, userId);
            return RedirectToAction(nameof(GetPackingListModal), new { tripId = tripId });
        }

        [HttpPost]
        public async Task<IActionResult> SyncPackingList(int tripId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _packingService.SyncWithDefaultListAsync(tripId, userId);
            return RedirectToAction(nameof(GetPackingListModal), new { tripId = tripId });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPackingList(int tripId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _packingService.ResetToDefaultListAsync(tripId, userId);
            return RedirectToAction(nameof(GetPackingListModal), new { tripId = tripId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTripPackingItem(int itemId, int tripId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _packingService.DeleteTripItemAsync(itemId, userId);
            return RedirectToAction(nameof(GetPackingListModal), new { tripId = tripId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItineraryItem(int id, string title, string description)
        {
            var success = await _tripService.UpdateItineraryItemAsync(id, title, description, null); // Передайте час, якщо додали
            if (!success)
            {
                return BadRequest("Не вдалося оновити запис.");
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteItineraryItem(int id)
        {
            var success = await _tripService.DeleteItineraryItemAsync(id);
            if (!success)
            {
                return BadRequest("Не вдалося видалити запис.");
            }
            return Ok();
        }
    }
}
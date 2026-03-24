using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartTrip.Application.Interfaces;
using SmartTrip.Models;
using SmartTrip.UI.ViewModels;

namespace SmartTrip.UI.Controllers
{
    [Authorize] 
    public class TripController : Controller
    {
        private readonly ITripService _tripService;
        private readonly UserManager<User> _userManager;

        public TripController(ITripService tripService, UserManager<User> userManager)
        {
            _tripService = tripService;
            _userManager = userManager;
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

            await _tripService.CreateTripAsync(userId, model.DestinationName, model.StartDate, model.EndDate);

            return RedirectToAction("Index", "Home");
        }
    }
}

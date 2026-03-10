using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartTrip.Models;

namespace SmartTrip.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Home page requested by {UserName} at {RequestTime}",
                User.Identity?.Name ?? "Anonymous", DateTime.UtcNow);
            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogInformation("Privacy page accessed by {UserName}", 
                User.Identity?.Name ?? "Anonymous");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Error page displayed. RequestId: {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}

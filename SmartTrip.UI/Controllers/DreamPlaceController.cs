using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrip.Data;
using SmartTrip.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartTrip.UI.Controllers
{
    [Authorize]
    public class DreamPlaceController : Controller
    {
        private readonly SmartTripDbContext _context;

        public DreamPlaceController(SmartTripDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var places = await _context.DreamPlaces.Where(dp => dp.UserId == userId).ToListAsync();
            return View(places);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DreamPlace dreamPlace, IFormFile? photoFile)
        {
            // Set required UserId
            dreamPlace.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                if (photoFile != null && photoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "dreamplaces");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + photoFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(fileStream);
                    }
                    dreamPlace.PhotoUrl = "/images/dreamplaces/" + uniqueFileName;
                }

                _context.Add(dreamPlace);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(dreamPlace);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var dreamPlace = await _context.DreamPlaces.FirstOrDefaultAsync(dp => dp.Id == id && dp.UserId == userId);

            if (dreamPlace == null) return NotFound();

            return View(dreamPlace);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DreamPlace dreamPlace, IFormFile? photoFile)
        {
            if (id != dreamPlace.Id) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            dreamPlace.UserId = userId; // ensure user can't spoof

            if (ModelState.IsValid)
            {
                try
                {
                    if (photoFile != null && photoFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "dreamplaces");
                        Directory.CreateDirectory(uploadsFolder);
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + photoFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await photoFile.CopyToAsync(fileStream);
                        }
                        dreamPlace.PhotoUrl = "/images/dreamplaces/" + uniqueFileName;
                    }

                    _context.Update(dreamPlace);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DreamPlaceExists(dreamPlace.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(dreamPlace);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var dreamPlace = await _context.DreamPlaces.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            
            if (dreamPlace == null) return NotFound();

            return View(dreamPlace);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var dreamPlace = await _context.DreamPlaces.FirstOrDefaultAsync(dp => dp.Id == id && dp.UserId == userId);
            
            if (dreamPlace != null)
            {
                _context.DreamPlaces.Remove(dreamPlace);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var dreamPlace = await _context.DreamPlaces.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            
            if (dreamPlace == null) return NotFound();

            return View(dreamPlace);
        }

        private bool DreamPlaceExists(int id)
        {
            return _context.DreamPlaces.Any(e => e.Id == id);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using SmartTrip.Application.Interfaces;
using SmartTrip.Models;
using SmartTrip.UI.ViewModels;
using System.Security.Claims;
using System.IO;

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

        [HttpGet]
        public async Task<IActionResult> ExportProfilePdf()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _profileService.GetUserProfileAsync(userId!);

            if (user == null)
                return NotFound();

            var defaultItems = await _packingService.GetDefaultItemsAsync(userId!);

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                DefaultPackingItems = defaultItems
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);

                    page.Header().AlignCenter().Text("Мій профіль").FontSize(24).Bold();

                    page.Content().Column(column =>
                    {
                        column.Spacing(15);

                        // Секція профіля
                        column.Item().Row(row =>
                        {
                            // Фото профіля
                            row.ConstantItem(150).Column(col =>
                            {
                                if (!string.IsNullOrEmpty(model.ProfileImageUrl))
                                {
                                    col.Item().Image(GetImageBytes(model.ProfileImageUrl)).FitWidth();
                                }
                                else
                                {
                                    col.Item().PaddingVertical(50).AlignCenter().Text("📷").FontSize(60);
                                }
                            });

                            // Інформація профіля
                            row.RelativeItem().Column(col =>
                            {
                                col.Spacing(8);

                                col.Item().Text("Персональні дані").FontSize(14).Bold().FontColor(Colors.Blue.Medium);

                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Ім'я:").Bold();
                                    r.RelativeItem().Text(model.FirstName ?? "-");
                                });

                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Прізвище:").Bold();
                                    r.RelativeItem().Text(model.LastName ?? "-");
                                });

                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Email:").Bold();
                                    r.RelativeItem().Text(model.Email ?? "-");
                                });
                            });
                        });

                        // Розділювач
                        column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Базовий чеклист
                        if (defaultItems != null && defaultItems.Any())
                        {
                            column.Item().Text("Мій базовий чеклист").FontSize(14).Bold().FontColor(Colors.Blue.Medium);

                            var groupedItems = defaultItems.GroupBy(x => x.Category);

                            foreach (var group in groupedItems)
                            {
                                column.Item().Text($"📋 {group.Key}").FontSize(12).Bold().FontColor(Colors.Grey.Darken1);

                                foreach (var item in group)
                                {
                                    column.Item().PaddingLeft(15).Text($"• {item.Name}").FontSize(11);
                                }

                                column.Item().PaddingBottom(5);
                            }
                        }
                        else
                        {
                            column.Item().Text("Чеклист порожній").FontSize(11).Italic().FontColor(Colors.Grey.Medium);
                        }
                    });

                    page.Footer().AlignCenter().Text($"Згенеровано SmartTrip - {DateTime.Now:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", $"Профіль_{model.FirstName}_{model.LastName}.pdf");
        }

        private byte[] GetImageBytes(string imagePath)
        {
            try
            {
                if (imagePath.StartsWith("http://") || imagePath.StartsWith("https://"))
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var response = client.GetAsync(imagePath).Result;
                        return response.Content.ReadAsByteArrayAsync().Result;
                    }
                }
                else
                {
                    var fullPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        return System.IO.File.ReadAllBytes(fullPath);
                    }
                }
            }
            catch
            {
                // Якщо помилка при завантаженні фото - повертаємо пусте
            }
            return new byte[0];
        }
    }
}
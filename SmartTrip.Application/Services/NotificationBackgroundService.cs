using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartTrip.Data;
using SmartTrip.Application.Interfaces;

namespace SmartTrip.Application.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly INotificationSender _notificationSender;
        private readonly ILogger<NotificationBackgroundService> _logger;

        private readonly HashSet<int> _notifiedUpcomingTrips = new();
        private readonly HashSet<int> _notifiedPassedTrips = new();
        private readonly HashSet<string> _notifiedEvents = new();   

        public NotificationBackgroundService(
            IServiceProvider serviceProvider, 
            INotificationSender notificationSender,
            ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _notificationSender = notificationSender;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");

            // Додано: даємо UI час підключитися до SignalR
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotificationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred checking notifications.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task CheckAndSendNotificationsAsync(CancellationToken stoppingToken)
        {
            // Оскільки DbContext є Scoped, потрібно створювати новий scope для фонового сервісу
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SmartTripDbContext>();
            var now = DateTime.UtcNow;

            // Подія 1: Наближення подорожі
            DateTime tomorrow = now.AddDays(1).Date;
            Console.WriteLine($"[Бекграунд] Шукаю події на {tomorrow:yyyy-MM-dd}");
            
            var upcomingTrips = await dbContext.Trips
                .Include(t => t.City)
                // Можливо, у вашій БД дати збережені з часом? Краще порівнювати через >= і <
                .Where(t => t.StartDate >= tomorrow && t.StartDate < tomorrow.AddDays(1))
                .ToListAsync(stoppingToken);

            Console.WriteLine($"[Бекграунд] Знайдено подорожей на завтра: {upcomingTrips.Count}");

            foreach (var trip in upcomingTrips)
            {
                if (_notifiedUpcomingTrips.Add(trip.Id))
                {
                    Console.WriteLine($"[Бекграунд] Надсилаю сповіщення для User {trip.UserId}");
                    await _notificationSender.SendNotificationAsync(trip.UserId, "Рюкзак зібрано?", $"Ваша подорож до {trip.City?.Name} починається вже завтра!");
                }
            }

            // Подія 2: Подорож завершено (Нагадування залишити рейтинг)
            var passedTrips = await dbContext.Trips
                .Include(t => t.City)
                .Where(t => t.EndDate.Date == now.AddDays(-1).Date && t.Rating == null)
                .ToListAsync(stoppingToken);

            foreach (var trip in passedTrips)
            {
                if (_notifiedPassedTrips.Add(trip.Id))
                {
                    await _notificationSender.SendNotificationAsync(trip.UserId, "Як поїздка?", $"Сподіваємось, вам сподобалось у {trip.City?.Name}! Не забудьте оцінити вашу подорож.");
                }
            }

            // Подія 3: Локальні події (Нові івенти знайдено у місті, під час поточної подорожі)
            var currentTrips = await dbContext.Trips
                .Where(t => t.StartDate <= now && t.EndDate >= now)
                .ToListAsync(stoppingToken);

            foreach (var trip in currentTrips)
            {
                var localEvents = await dbContext.LocalEvents
                    .Where(le => le.CityId == trip.CityId && le.EventDate.Date == now.Date)
                    .ToListAsync(stoppingToken);

                foreach (var ev in localEvents)
                {
                    string eventHashId = $"{trip.UserId}-{ev.Id}";
                    if (_notifiedEvents.Add(eventHashId))
                    {
                        await _notificationSender.SendNotificationAsync(trip.UserId, "Подія поруч!", $"У місті проходить подія: {ev.Title}. Не забудьте відвідати!");
                    }
                }
            }
        }
    }
}
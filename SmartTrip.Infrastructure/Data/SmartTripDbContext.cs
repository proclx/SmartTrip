using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SmartTrip.Models;

namespace SmartTrip.Data
{
    public class SmartTripDbContext : IdentityDbContext<User>
    {
        public SmartTripDbContext(DbContextOptions<SmartTripDbContext> options) : base(options)
        {
        }

        public DbSet<City> Cities { get; set; }
        public DbSet<Place> Places { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripDay> TripDays { get; set; }
        public DbSet<ItineraryItem> ItineraryItems { get; set; }

        // Додаємо нову таблицю для галереї
        public DbSet<Photo> Photos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Налаштування зв'язку: якщо видаляємо подорож — видаляються і записи про фото в БД
            builder.Entity<Photo>()
                .HasOne(p => p.Trip)
                .WithMany(t => t.Photos)
                .HasForeignKey(p => p.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // Налаштування зв'язку з користувачем
            builder.Entity<Photo>()
                .HasOne(p => p.User)
                .WithMany(u => u.Photos)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Не видаляємо юзера, якщо видалено фото
        }
    }
}
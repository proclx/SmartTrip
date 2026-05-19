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

        public DbSet<Photo> Photos { get; set; }

        public DbSet<DreamPlace> DreamPlaces { get; set; }

        public DbSet<DefaultPackingItem> DefaultPackingItems { get; set; }
        public DbSet<TripPackingItem> TripPackingItems { get; set; }

        public DbSet<LocalEvent> LocalEvents { get; set; } 

        public DbSet<VotingSession> VotingSessions { get; set; }
        public DbSet<VotingItem> VotingItems { get; set; }
        public DbSet<Vote> Votes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //  якщо видаляємо подорож — видаляються і записи про фото в БД
            builder.Entity<Photo>()
                .HasOne(p => p.Trip)
                .WithMany(t => t.Photos)
                .HasForeignKey(p => p.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Photo>()
                .HasOne(p => p.User)
                .WithMany(u => u.Photos)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Не видаляємо юзера, якщо видалено фото

            builder.Entity<TripPackingItem>()    
                .HasOne(p => p.Trip)  
                .WithMany(t => t.PackingItems) 
                .HasForeignKey(p => p.TripId) 
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
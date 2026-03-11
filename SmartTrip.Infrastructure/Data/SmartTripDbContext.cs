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
    
    }
}

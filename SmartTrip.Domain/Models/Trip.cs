using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartTrip.Models
{
    public class Trip
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public int CityId { get; set; }
        [ForeignKey(nameof(CityId))]
        public City? City { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int PeopleCount { get; set; } = 1;

        public int? Rating { get; set; }

        public bool IsFavorite { get; set; } = false;

        public string? StartingPoint { get; set; }
        public string? RouteToDestination { get; set; }
        public string? RouteBack { get; set; }

        public List<TripDay> TripDays { get; set; } = new();
        public List<Photo> Photos { get; set; } = new();

        public List<TripPackingItem> PackingItems { get; set; } = new();
        public string? Notes { get; set; } // Нотатки до подорожі
    }
}

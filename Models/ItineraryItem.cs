using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrip.Models
{
    public class ItineraryItem
    {
        public int Id { get; set; }

        public int TripDayId { get; set; }
        [ForeignKey(nameof(TripDayId))]
        public TripDay? TripDay { get; set; }

        public int PlaceId { get; set; }
        [ForeignKey(nameof(PlaceId))]
        public Place? Place { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public string? Notes { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrip.Models
{
    public class TripDay
    {
        public int Id { get; set; }

        public int TripId { get; set; }
        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }

        public DateTime Date { get; set; }

        public int DayNumber { get; set; }
        public List<ItineraryItem> ItineraryItems { get; set; } = new();
    }
}

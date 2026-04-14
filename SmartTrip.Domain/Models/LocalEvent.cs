using System;

namespace SmartTrip.Models
{
    public class LocalEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string LocationUrl { get; set; } = string.Empty;
        public decimal? TicketPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        // «в'€зок з м≥стом
        public int CityId { get; set; }
        public City City { get; set; } = null!;
    }
}
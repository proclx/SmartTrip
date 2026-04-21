using System;
using System.Collections.Generic;

namespace SmartTrip.UI.ViewModels
{
    public class ItineraryViewModel
    {
        public int TripId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? StartingPoint { get; set; }
        public string? RouteToDestination { get; set; }
        public string? RouteBack { get; set; }

        public List<ItineraryDayViewModel> Days { get; set; } = new();
    }

    public class ItineraryDayViewModel
    {
        public int DayIndex { get; set; }
        public DateTime Date { get; set; }
        public List<ItineraryItemViewModel> Items { get; set; } = new();
    }

    public class ItineraryItemViewModel
    {
        public int Id { get; set; } 
        
        public string PlaceName { get; set; } = string.Empty;
        public string PlaceType { get; set; } = string.Empty; // "Hotel" | "Restaurant" | "Attraction"
        public double? Rating { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace SmartTrip.UI.ViewModels
{

    public class TripPhotoViewModel
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }

    public class TripViewModel
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PeopleCount { get; set; }
        public int? Rating { get; set; }
        public List<TripPhotoViewModel>? Photos { get; set; }
        public int PhotoCount => Photos?.Count ?? 0;
        public bool IsFavorite { get; set; }
        public IEnumerable<SmartTrip.Models.LocalEvent> SuggestedEvents { get; set; } = new List<SmartTrip.Models.LocalEvent>();
        public string? Notes { get; set; }
        public bool IsArchived { get; set; }
    }

    public class EditTripViewModel
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Кількість людей повинна бути від 1 до 100")]
        [Display(Name = "Кількість людей")]
        public int PeopleCount { get; set; }

        [Range(1, 5, ErrorMessage = "Рейтинг має бути від 1 до 5")]
        [Display(Name = "Рейтинг")]
        public int? Rating { get; set; }
        public string? Notes { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrip.Models
{
    public class DreamPlace
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? PhotoUrl { get; set; }

        public string? LocationInfo { get; set; }

        public string UserId { get; set; } = string.Empty;
    }
}

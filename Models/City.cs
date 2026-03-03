using System.ComponentModel.DataAnnotations;

namespace SmartTrip.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<Place> Places { get; set; } = new();
    }
}

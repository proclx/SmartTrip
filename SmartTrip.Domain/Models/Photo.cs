using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrip.Models 
{
    public class Photo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        // Зв'язок з користувачем (Identity User)
        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public User? User { get; set; }

        // Зв'язок з подорожжю
        [Required]
        public int TripId { get; set; }
        [ForeignKey("TripId")]
        public Trip? Trip { get; set; }
    }
}
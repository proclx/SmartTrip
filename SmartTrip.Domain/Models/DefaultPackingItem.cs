using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrip.Models
{
    public class DefaultPackingItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required(ErrorMessage = "Назва речі є обов'язковою")]
        public string Name { get; set; } = string.Empty;

        // Категорія за замовчуванням
        public string Category { get; set; } = "Інше";
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrip.Models
{
    public class TripPackingItem
    {
        public int Id { get; set; }

        public int TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }

        [Required(ErrorMessage = "Назва речі є обов'язковою")]
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = "Інше";

        // Статус: чи покладено у валізу
        public bool IsChecked { get; set; } = false;
    }
}
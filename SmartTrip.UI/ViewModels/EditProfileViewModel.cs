using System.ComponentModel.DataAnnotations;

namespace SmartTrip.UI.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Ім'я обов'язкове")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Прізвище обов'язкове")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Емейл обов'язковий")]
        [EmailAddress(ErrorMessage = "Невірна адреса електронної пошти")]
        public string? Email { get; set; }

        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Завантажити нове фото")]
        public IFormFile? ImageFile { get; set; }
        public IEnumerable<SmartTrip.Models.DefaultPackingItem> DefaultPackingItems { get; set; } = new List<SmartTrip.Models.DefaultPackingItem>();
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartTrip.UI.ViewModels
{
    public class UploadPhotoViewModel
    {
        [Required(ErrorMessage = "Будь ласка, оберіть подорож")]
        [Display(Name = "Подорож")]
        public int SelectedTripId { get; set; }

        [Required(ErrorMessage = "Оберіть хоча б одне фото")]
        [Display(Name = "Фотографії")]
        public List<IFormFile> Files { get; set; } = new();

        // Список для випадаючого меню (Select List)
        public IEnumerable<SelectListItem>? Trips { get; set; }
    }
}
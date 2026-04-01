using System.ComponentModel.DataAnnotations;

namespace SmartTrip.UI.ViewModels
{
    public class CreateTripViewModel
    {
        [Required(ErrorMessage = "Введіть місто для подорожі")]
        [Display(Name = "Місто (наприклад: Париж)")]
        public string DestinationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть точку відправлення")]
        [Display(Name = "Звідки ви їдете? (наприклад: Київ)")]
        public string StartingPoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть дату початку")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата початку")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Вкажіть дату завершення")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата завершення")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(3);
    }
}

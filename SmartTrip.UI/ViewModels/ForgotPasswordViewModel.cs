using System.ComponentModel.DataAnnotations;

namespace SmartTrip.UI.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Невірний формат Email")]
        public string Email { get; set; } = string.Empty;
    }
}
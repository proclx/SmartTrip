using System.ComponentModel.DataAnnotations;

namespace SmartTrip.UI.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Введіть поточний пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Поточний пароль")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть новий пароль")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Пароль має містити мінімум {2} символів.", MinimumLength = 6)]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z\\d]).+$", ErrorMessage = "Пароль має містити мінімум 6 символів, одну велику літеру, одну маленьку літеру, цифру та спеціальний символ.")]
        [Display(Name = "Новий пароль")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Підтвердіть новий пароль")]
        [Compare("NewPassword", ErrorMessage = "Паролі не співпадають")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

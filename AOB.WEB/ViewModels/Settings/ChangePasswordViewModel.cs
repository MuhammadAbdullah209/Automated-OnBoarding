using System.ComponentModel.DataAnnotations;
using AOB.WEB.Validation;

namespace AOB.WEB.ViewModels.Settings
{
    /// <summary>Backing model for the change-password form. Uses the shared [PasswordPolicy] rules for the new password.</summary>
    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, PasswordPolicy, DataType(DataType.Password), Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Confirm new password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords don't match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

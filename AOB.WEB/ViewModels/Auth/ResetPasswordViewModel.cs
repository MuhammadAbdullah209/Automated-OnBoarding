using System.ComponentModel.DataAnnotations;
using AOB.WEB.Validation;

namespace AOB.WEB.ViewModels.Auth
{
    /// <summary>
    /// Backing model for the new-password forms: step 3 of forgot-password, and the change-temporary-password
    /// form shown after a login with a temporary password.
    /// </summary>
    public class ResetPasswordViewModel
    {
        // Both are carried as hidden fields from the previous step (OTP verification, or the temporary-password login).
        [Required, EmailAddress, StringLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string ResetToken { get; set; } = string.Empty;

        [Required, PasswordPolicy, DataType(DataType.Password), Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

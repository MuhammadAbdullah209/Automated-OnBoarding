using System.ComponentModel.DataAnnotations;
using AOB.WEB.Validation;

namespace AOB.WEB.ViewModels.Admin
{
    /// <summary>Backing model for the admin forms that create an internal user or a partner.</summary>
    public class CreateAccountViewModel
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required, PasswordPolicy, DataType(DataType.Password), Display(Name = "Initial password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

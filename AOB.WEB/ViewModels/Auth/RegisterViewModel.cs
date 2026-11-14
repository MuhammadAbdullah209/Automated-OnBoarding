using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Constants;
using AOB.WEB.Validation;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.ViewModels.Auth
{
    /// <summary>Backing model for the create-account form (direct customer sign-up).</summary>
    public class RegisterViewModel
    {
        [Required, StringLength(200), Display(Name = "Company name")]
        public string CompanyName { get; set; } = string.Empty;

        // Remote: checked against every account as the user types (AuthController.IsEmailAvailable); Register re-checks.
        [Required, EmailAddress, StringLength(320), Display(Name = "Work email")]
        [RegularExpression(ValidationMessages.EmailPattern, ErrorMessage = ValidationMessages.EmailFormat)]
        [Remote("IsEmailAvailable", "Auth", ErrorMessage = ValidationMessages.EmailInUse)]
        public string Email { get; set; } = string.Empty;

        [Required, PasswordPolicy, DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

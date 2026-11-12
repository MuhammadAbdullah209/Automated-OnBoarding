using System.ComponentModel.DataAnnotations;

namespace AOB.WEB.ViewModels.Auth
{
    /// <summary>Backing model for the login form.</summary>
    public class LoginViewModel
    {
        [Required, EmailAddress, Display(Name = "Work email")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in on this device")]
        public bool RememberMe { get; set; }

        // Where to go after sign-in; posted back as a hidden field.
        public string? ReturnUrl { get; set; }
    }
}

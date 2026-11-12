using System.ComponentModel.DataAnnotations;

namespace AOB.WEB.ViewModels.Auth
{
    /// <summary>Backing model for forgot-password and resend-verification forms (email only).</summary>
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress, StringLength(320), Display(Name = "Work email")]
        public string Email { get; set; } = string.Empty;
    }
}

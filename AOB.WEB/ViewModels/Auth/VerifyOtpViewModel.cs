using System.ComponentModel.DataAnnotations;

namespace AOB.WEB.ViewModels.Auth
{
    /// <summary>Backing model for the OTP verification form (step 2 of forgot-password).</summary>
    public class VerifyOtpViewModel
    {
        // Carried through as a hidden field from the forgot-password step.
        [Required, EmailAddress, StringLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(10), Display(Name = "One-time code")]
        public string Otp { get; set; } = string.Empty;
    }
}

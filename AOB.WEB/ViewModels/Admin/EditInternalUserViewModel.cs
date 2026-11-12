using System.ComponentModel.DataAnnotations;

namespace AOB.WEB.ViewModels.Admin
{
    /// <summary>Backing model for editing an internal user's name and login email.</summary>
    public class EditInternalUserViewModel
    {
        [Required, StringLength(200), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(320), Display(Name = "Login email")]
        public string Email { get; set; } = string.Empty;
    }
}

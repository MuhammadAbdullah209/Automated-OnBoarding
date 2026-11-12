using System.ComponentModel.DataAnnotations;

namespace AOB.WEB.ViewModels.Partners
{
    /// <summary>Backing model for editing a partner's name and login email.</summary>
    public class EditPartnerViewModel
    {
        [Required, StringLength(200), Display(Name = "Partner name")]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(320), Display(Name = "Login email")]
        public string Email { get; set; } = string.Empty;
    }
}

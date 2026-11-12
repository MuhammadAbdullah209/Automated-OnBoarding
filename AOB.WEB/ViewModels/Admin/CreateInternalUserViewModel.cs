using System.ComponentModel.DataAnnotations;

namespace AOB.WEB.ViewModels.Admin
{
    /// <summary>
    /// Backing model for the super admin's "add internal user" form. No password: the user verifies
    /// their email and is then emailed a temporary password, which they must change at first sign-in.
    /// </summary>
    public class CreateInternalUserViewModel
    {
        [Required, StringLength(200), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(320)]
        public string Email { get; set; } = string.Empty;
    }
}

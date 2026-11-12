namespace AOB.BAL.DTOs.Auth
{
    /// <summary>Data needed to change the signed-in user's password.</summary>
    public class ChangePasswordDto
    {
        /// <summary>Always taken from the authenticated user's claims, never from the form.</summary>
        public Guid UserAuthId { get; set; }

        public string CurrentPassword { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}

namespace AOB.BAL.DTOs.Auth
{
    /// <summary>Data an admin supplies to create an internal user or a partner login.</summary>
    public class CreateAccountDto
    {
        /// <summary>Full name for an internal user, organisation name for a partner.</summary>
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

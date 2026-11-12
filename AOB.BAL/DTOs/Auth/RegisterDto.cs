namespace AOB.BAL.DTOs.Auth
{
    /// <summary>Data required for a direct customer to create an account.</summary>
    public class RegisterDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

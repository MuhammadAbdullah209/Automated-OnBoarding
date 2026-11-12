namespace AOB.BAL.DTOs.Auth
{
    /// <summary>Credentials submitted at sign-in.</summary>
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

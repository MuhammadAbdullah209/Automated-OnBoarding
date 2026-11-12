namespace AOB.BAL.DTOs.Auth
{
    /// <summary>
    /// Data the super admin supplies to create an internal user. There is deliberately no password:
    /// the user verifies their email and is then emailed a system-generated temporary password.
    /// </summary>
    public class CreateInternalUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

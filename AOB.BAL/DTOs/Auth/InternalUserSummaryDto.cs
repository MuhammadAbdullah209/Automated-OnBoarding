namespace AOB.BAL.DTOs.Auth
{
    public class InternalUserSummaryDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        /// <summary>False until the user clicks the link in the verification email.</summary>
        public bool EmailConfirmed { get; set; }
        public bool IsSuperAdmin { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}

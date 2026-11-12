using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Auth
{
    /// <summary>Safe projection of a signed-in user (no password hash).</summary>
    public class AuthenticatedUserDto
    {
        public Guid UserId { get; set; }
        /// <summary>Name shown in the UI: full name, partner name, or company name (email if none).</summary>
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserType UserType { get; set; }

        /// <summary>Set for Partner users and Customer users; null for Internal users.</summary>
        public Guid? PartnerId { get; set; }

        /// <summary>Set for Customer users only.</summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// True when the password used is a temporary one. Such a login is not a sign-in: no session is
        /// started; the user must first set a new password with <see cref="PasswordChangeToken"/>.
        /// </summary>
        public bool MustChangePassword { get; set; }

        /// <summary>One-time, short-lived grant to set a new password; only set when MustChangePassword is true.</summary>
        public string? PasswordChangeToken { get; set; }
    }
}

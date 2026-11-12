using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// A one-time secret issued to a user: either the email-verification link token or a
    /// password-reset OTP. Only a hash of the secret is stored, never the secret itself.
    /// </summary>
    public class OtpToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Auth))]
        public Guid UserAuthId { get; set; }

        public OtpPurpose Purpose { get; set; }

        // SHA-256 (hex) of the token/OTP.
        [Required, MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? ConsumedAt { get; set; }

        public int FailedAttempts { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public UserAuth Auth { get; set; } = null!;
    }
}

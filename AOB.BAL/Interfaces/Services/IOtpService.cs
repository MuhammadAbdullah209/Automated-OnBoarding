namespace AOB.BAL.Interfaces.Services
{
    /// <summary>
    /// Issues and validates one-time secrets stored in the OtpTokens table.
    /// Only hashes are persisted; the plain secret is returned once, to be emailed.
    /// </summary>
    public interface IOtpService
    {
        /// <summary>Creates an email-verification link token (invalidating older ones) and returns it.</summary>
        Task<string> CreateEmailVerificationTokenAsync(Guid userAuthId);

        /// <summary>
        /// Validates and consumes an email-verification token.
        /// Returns the owning UserAuth id, or null if the token is unknown, used or expired.
        /// </summary>
        Task<Guid?> ConsumeEmailVerificationTokenAsync(string token);

        /// <summary>Creates a password-reset OTP (invalidating older ones) and returns the plain code.</summary>
        Task<string> CreatePasswordResetOtpAsync(Guid userAuthId);

        /// <summary>
        /// Checks a password-reset OTP. A correct code is consumed; a wrong one counts towards
        /// the attempt limit, after which the OTP is dead.
        /// </summary>
        Task<bool> ConsumePasswordResetOtpAsync(Guid userAuthId, string otp);

        /// <summary>Creates the grant that authorises setting a new password after the OTP was verified.</summary>
        Task<string> CreatePasswordResetGrantAsync(Guid userAuthId);

        /// <summary>Validates and consumes a password-reset grant for the user.</summary>
        Task<bool> ConsumePasswordResetGrantAsync(Guid userAuthId, string grant);

        /// <summary>True when too many reset codes were issued to this account in the last hour (a new code would reset its attempt limit).</summary>
        Task<bool> IsPasswordResetThrottledAsync(Guid userAuthId);

        /// <summary>True when too many verification links were issued to this account in the last hour.</summary>
        Task<bool> IsEmailVerificationThrottledAsync(Guid userAuthId);

        /// <summary>Lifetime of a password-reset OTP, for use in the email text.</summary>
        int PasswordResetOtpExpiryMinutes { get; }

        /// <summary>Lifetime of a verification link, for use in the email text.</summary>
        int EmailVerificationExpiryHours { get; }
    }
}

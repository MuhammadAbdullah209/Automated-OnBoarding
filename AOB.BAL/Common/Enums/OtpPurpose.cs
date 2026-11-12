namespace AOB.BAL.Common.Enums
{
    /// <summary>What a row in the OtpTokens table was issued for.</summary>
    public enum OtpPurpose
    {
        /// <summary>Long random token embedded in the verification link emailed after sign-up.</summary>
        EmailVerification = 1,

        /// <summary>Short numeric one-time password emailed for "forgot password".</summary>
        PasswordReset = 2,

        /// <summary>
        /// Issued once a password-reset OTP has been verified; the "set new password" step must
        /// present it. Keeps OTP verification and password entry as two separate steps.
        /// </summary>
        PasswordResetGrant = 3
    }
}

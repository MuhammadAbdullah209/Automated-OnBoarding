namespace AOB.BAL.DTOs.Auth
{
    /// <summary>Data needed for the final "set new password" step of forgot-password.</summary>
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;

        /// <summary>The grant returned by IAuthService.VerifyPasswordResetOtpAsync.</summary>
        public string ResetToken { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}

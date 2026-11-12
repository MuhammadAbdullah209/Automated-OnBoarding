using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Auth;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Business rules for registration, email verification, sign-in and password reset.</summary>
    public interface IAuthService
    {
        /// <summary>
        /// Self-registration of a direct customer: creates UserAuth + Customer
        /// (IsDirect = true, owned by PCIPAL) and emails a verification link.
        /// Fails if the email is already taken.
        /// </summary>
        Task<ServiceResult> RegisterDirectCustomerAsync(RegisterDto dto);

        /// <summary>True if no account of any type uses the email yet (for the sign-up form's live check).</summary>
        Task<bool> IsEmailAvailableAsync(string email);

        /// <summary>
        /// Confirms the email for the account that owns the token from the emailed link.
        /// For an internal user this also generates a temporary password and emails it; if that email
        /// cannot be sent the account goes back to unverified so a new link can be requested.
        /// Data is true when a temporary password was emailed.
        /// </summary>
        Task<ServiceResult<bool>> VerifyEmailAsync(string token);

        /// <summary>
        /// Issues a new verification link for an unverified, active account and emails it.
        /// Unlike <see cref="ResendVerificationEmailAsync"/> this reports failures, for admin screens.
        /// </summary>
        Task<ServiceResult> SendVerificationEmailAsync(Guid userAuthId);

        /// <summary>
        /// Re-sends the verification link. Always succeeds from the caller's view, so it
        /// cannot be used to discover which emails are registered.
        /// </summary>
        Task<ServiceResult> ResendVerificationEmailAsync(string email);

        /// <summary>
        /// Validates credentials (with lockout, email must be verified) and returns the user.
        /// If the password is a temporary one (MustChangePassword), the result is not a sign-in: it carries
        /// a short-lived PasswordChangeToken for <see cref="ChangeTemporaryPasswordAsync"/> and no session may be started.
        /// </summary>
        Task<ServiceResult<AuthenticatedUserDto>> LoginAsync(LoginDto dto);

        /// <summary>
        /// Replaces a temporary password using the PasswordChangeToken from <see cref="LoginAsync"/> (dto.ResetToken)
        /// and clears MustChangePassword. The user then signs in again with the new password.
        /// </summary>
        Task<ServiceResult> ChangeTemporaryPasswordAsync(ResetPasswordDto dto);

        /// <summary>
        /// Emails a password-reset OTP if the account exists. Always succeeds from the caller's
        /// view, so it cannot be used to discover which emails are registered.
        /// </summary>
        Task<ServiceResult> ForgotPasswordAsync(string email);

        /// <summary>
        /// Step 2 of forgot-password: checks the emailed OTP. On success returns a short-lived
        /// reset token that the final step must present.
        /// </summary>
        Task<ServiceResult<string>> VerifyPasswordResetOtpAsync(string email, string otp);

        /// <summary>Step 3 of forgot-password: sets the new password using the reset token.</summary>
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto);

        /// <summary>
        /// Changes the password of the signed-in user after verifying the current one.
        /// Wrong current passwords count toward the same lockout as sign-in.
        /// Also clears MustChangePassword, ending the temporary-password restriction.
        /// </summary>
        Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto dto);
    }
}

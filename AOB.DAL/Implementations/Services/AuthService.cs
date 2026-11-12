using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Auth;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Registration and sign-in business logic. UserAuth is the only authentication table;
    /// the profile table is chosen by UserAuth.UserType.
    /// </summary>
    public class AuthService(
        IUserAuthRepository userAuthRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        IOtpService otpService,
        IEmailService emailService,
        IActivityLogService activityLog,
        ILogger<AuthService> logger) : IAuthService
    {
        private readonly int _maxFailedAttempts = configuration.GetValue("AuthConfig:MaxFailedAttempts", 5);
        private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(configuration.GetValue("AuthConfig:LockoutDurationInMinutes", 15));

        public async Task<ServiceResult> RegisterDirectCustomerAsync(RegisterDto dto)
        {
            var normalizedEmail = dto.Email.NormalizeEmail();

            // Checked against every account (not just customers): sign-in finds accounts by email alone.
            if (await userAuthRepository.EmailExistsAsync(normalizedEmail))
            {
                return ServiceResult.Failure(ValidationMessages.EmailInUse);
            }

            // Direct customers always belong to PCIPAL.
            var auth = new UserAuth
            {
                Email = dto.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = passwordHasher.Hash(dto.Password),
                UserType = UserType.Customer,
                Customer = new Customer
                {
                    CompanyName = dto.CompanyName.Trim(),
                    IsDirect = true,
                    PartnerId = PartnerConstants.PcipalPartnerId
                }
            };

            // Single SaveChanges = one transaction for both rows.
            await userAuthRepository.AddAsync(auth);
            await unitOfWork.SaveChangesAsync();

            await activityLog.LogCustomerAsync(auth.Customer.Id, "Customer registered", "Signed up directly and was assigned to PCIPAL.");
            
            // The account exists even if the email fails; the user can request another link.
            await TrySendVerificationEmailAsync(auth);

            return ServiceResult.Success();
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
            => !string.IsNullOrWhiteSpace(email) && !await userAuthRepository.EmailExistsAsync(email.NormalizeEmail());

        public async Task<ServiceResult<bool>> VerifyEmailAsync(string token)
        {
            const string invalid = "This verification link is invalid or has expired.";

            var userAuthId = await otpService.ConsumeEmailVerificationTokenAsync(token);
            if (userAuthId is null)
            {
                return ServiceResult<bool>.Failure(invalid);
            }

            var auth = await userAuthRepository.GetByIdAsync(userAuthId.Value);
            if (auth is null)
            {
                return ServiceResult<bool>.Failure(invalid);
            }

            if (auth.UserType == UserType.InternalUser)
            {
                return await CompleteInternalUserVerificationAsync(auth);
            }

            auth.EmailConfirmed = true;
            auth.UpdatedAt = DateTimeOffset.UtcNow;
            throw new InvalidOperationException("Test Exception");
            await unitOfWork.SaveChangesAsync();

            return ServiceResult<bool>.Success(false);
        }

        public async Task<ServiceResult> SendVerificationEmailAsync(Guid userAuthId)
        {
            var auth = await userAuthRepository.GetByIdAsync(userAuthId);
            if (auth is not { EmailConfirmed: false, IsActive: true })
            {
                return ServiceResult.Failure("This account is already verified or is inactive.");
            }

            return await TrySendVerificationEmailAsync(auth)
                ? ServiceResult.Success()
                : ServiceResult.Failure("The verification email could not be sent. Please try again later.");
        }

        public async Task<ServiceResult> ResendVerificationEmailAsync(string email)
        {
            var auth = await userAuthRepository.GetByNormalizedEmailAsync(email.NormalizeEmail());
            if (auth is { EmailConfirmed: false, IsActive: true } && !await otpService.IsEmailVerificationThrottledAsync(auth.Id))
            {
                await TrySendVerificationEmailAsync(auth);
            }

            // Same outcome whether or not the account exists.
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ForgotPasswordAsync(string email)
        {
            var auth = await userAuthRepository.GetByNormalizedEmailAsync(email.NormalizeEmail());
            if (auth is { IsActive: true })
            {
                // Each new code resets the attempt budget of the one before, so the number of codes is capped too.
                try
                {
                    if (await otpService.IsPasswordResetThrottledAsync(auth.Id))
                    {
                        logger.LogWarning("SecurityEvent PasswordResetThrottled UserAuthId={UserAuthId}", auth.Id);
                        return ServiceResult.Success();
                    }
                }
                catch (Exception ex)
                {
                    // The throttle could not be checked (e.g. the database is unavailable). Fail closed: send no code,
                    // so the per-hour cap cannot be bypassed, and answer the same way as always.
                    logger.LogError(ex, "Could not check the password reset throttle for user {UserAuthId}; no code was sent.", auth.Id);
                    return ServiceResult.Success();
                }

                try
                {
                    var otp = await otpService.CreatePasswordResetOtpAsync(auth.Id);
                    await emailService.SendTemplatedAsync(auth.Email, EmailTemplateType.PasswordResetOtp,
                        new Dictionary<string, string>
                        {
                            ["ConsumerName"] = GetDisplayName(auth),
                            ["OTP"] = otp,
                            ["OTPExpiryDuration"] = $"{otpService.PasswordResetOtpExpiryMinutes} minutes",
                            ["SupportContact"] = GetSupportContact()
                        });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send password reset OTP to user {UserAuthId}.", auth.Id);
                }
            }

            // Same outcome whether or not the account exists.
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<string>> VerifyPasswordResetOtpAsync(string email, string otp)
        {
            var auth = await userAuthRepository.GetByNormalizedEmailAsync(email.NormalizeEmail());
            if (auth is null || !await otpService.ConsumePasswordResetOtpAsync(auth.Id, otp.Trim()))
            {
                return ServiceResult<string>.Failure("The code is invalid or has expired.");
            }

            // OTP is now used up; the grant lets the user proceed to choose a password.
            var grant = await otpService.CreatePasswordResetGrantAsync(auth.Id);
            return ServiceResult<string>.Success(grant);
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            const string invalid = "Your reset session has expired. Please request a new code.";

            var auth = await userAuthRepository.GetByNormalizedEmailAsync(dto.Email.NormalizeEmail());
            if (auth is null || !await otpService.ConsumePasswordResetGrantAsync(auth.Id, dto.ResetToken))
            {
                return ServiceResult.Failure(invalid);
            }

            auth.PasswordHash = passwordHasher.Hash(dto.NewPassword);
            // Receiving the OTP proves ownership of the mailbox, and a reset clears any lockout.
            // The user chose this password themselves, so it is no longer a temporary one.
            auth.EmailConfirmed = true;
            auth.MustChangePassword = false;
            auth.AccessFailedCount = 0;
            auth.LockoutEnd = null;
            auth.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto dto)
        {
            var auth = await userAuthRepository.GetByIdAsync(dto.UserAuthId);
            if (auth is not { IsActive: true })
            {
                return ServiceResult.Failure("Your password could not be changed.");
            }

            if (auth.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow)
            {
                return ServiceResult.Failure("Account is temporarily locked. Try again later.");
            }

            // Same lockout bookkeeping as sign-in, so this form cannot be used to guess the password.
            if (!passwordHasher.Verify(dto.CurrentPassword, auth.PasswordHash))
            {
                auth.AccessFailedCount++;
                if (auth.AccessFailedCount >= _maxFailedAttempts)
                {
                    auth.LockoutEnd = DateTimeOffset.UtcNow.Add(_lockoutDuration);
                    auth.AccessFailedCount = 0;
                    logger.LogWarning("SecurityEvent AccountLockedOut UserAuthId={UserAuthId} Via=ChangePassword", auth.Id);
                }

                await unitOfWork.SaveChangesAsync();
                return ServiceResult.Failure("Current password is incorrect.");
            }

            if (dto.NewPassword == dto.CurrentPassword)
            {
                return ServiceResult.Failure("New password must be different from your current password.");
            }

            auth.PasswordHash = passwordHasher.Hash(dto.NewPassword);
            auth.MustChangePassword = false;
            auth.AccessFailedCount = 0;
            auth.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("SecurityEvent PasswordChanged UserAuthId={UserAuthId}", auth.Id);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<AuthenticatedUserDto>> LoginAsync(LoginDto dto)
        {
            const string invalid = "Invalid email or password.";

            var auth = await userAuthRepository.GetByNormalizedEmailAsync(dto.Email.NormalizeEmail());

            // Same message for unknown email and wrong password to avoid account enumeration.
            if (auth is null)
            {
                // Do the same hashing work as for a real account, so response time does not reveal whether it exists.
                passwordHasher.SpendVerifyTime(dto.Password);
                return ServiceResult<AuthenticatedUserDto>.Failure(invalid);
            }

            // A locked account answers like a wrong password (and after the same amount of work), so lockout does not reveal it exists.
            if (auth.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow)
            {
                passwordHasher.SpendVerifyTime(dto.Password);
                return ServiceResult<AuthenticatedUserDto>.Failure(invalid);
            }

            if (!passwordHasher.Verify(dto.Password, auth.PasswordHash))
            {
                auth.AccessFailedCount++;
                if (auth.AccessFailedCount >= _maxFailedAttempts)
                {
                    auth.LockoutEnd = DateTimeOffset.UtcNow.Add(_lockoutDuration);
                    auth.AccessFailedCount = 0;
                    logger.LogWarning("SecurityEvent AccountLockedOut UserAuthId={UserAuthId} Via=Login", auth.Id);
                }

                await unitOfWork.SaveChangesAsync();
                return ServiceResult<AuthenticatedUserDto>.Failure(invalid);
            }

            // Upgrade a hash made with older, weaker settings now that the plain password is at hand.
            if (passwordHasher.NeedsRehash(auth.PasswordHash))
            {
                auth.PasswordHash = passwordHasher.Hash(dto.Password);
            }

            if (!auth.IsActive)
            {
                return ServiceResult<AuthenticatedUserDto>.Failure("This account is disabled.");
            }

            if (!auth.EmailConfirmed)
            {
                return ServiceResult<AuthenticatedUserDto>.Failure(
                    "Please verify your email before signing in. Check your inbox for the verification link.");
            }

            var user = BuildUserDto(auth);
            if (user is null)
            {
                // UserAuth without a matching profile row is a data-integrity problem.
                return ServiceResult<AuthenticatedUserDto>.Failure("Account profile is incomplete. Contact support.");
            }

            if (auth.UserType == UserType.Customer && auth.Customer?.Status == CustomerStatus.Disabled)
            {
                return ServiceResult<AuthenticatedUserDto>.Failure(
                    "This customer account has been disabled. Contact support.");
            }

            auth.AccessFailedCount = 0;
            auth.LockoutEnd = null;

            if (auth.MustChangePassword)
            {
                // Not a sign-in: a correct temporary password only earns a short-lived, one-time grant
                // to set a new password (ChangeTemporaryPasswordAsync). The caller must not start a session.
                await unitOfWork.SaveChangesAsync();
                user.PasswordChangeToken = await otpService.CreatePasswordResetGrantAsync(auth.Id);
                return ServiceResult<AuthenticatedUserDto>.Success(user);
            }

            auth.LastLoginAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            return ServiceResult<AuthenticatedUserDto>.Success(user);
        }

        public async Task<ServiceResult> ChangeTemporaryPasswordAsync(ResetPasswordDto dto)
        {
            const string expired = "Your password change session has expired. Please sign in again with your temporary password.";

            var auth = await userAuthRepository.GetByNormalizedEmailAsync(dto.Email.NormalizeEmail());
            if (auth is not { MustChangePassword: true, IsActive: true, EmailConfirmed: true })
            {
                return ServiceResult.Failure(expired);
            }

            // Checked before the grant is used up, so the user can simply pick another password.
            if (passwordHasher.Verify(dto.NewPassword, auth.PasswordHash))
            {
                return ServiceResult.Failure("New password must be different from your temporary password.");
            }

            if (!await otpService.ConsumePasswordResetGrantAsync(auth.Id, dto.ResetToken))
            {
                return ServiceResult.Failure(expired);
            }

            auth.PasswordHash = passwordHasher.Hash(dto.NewPassword);
            auth.MustChangePassword = false;
            auth.AccessFailedCount = 0;
            auth.LockoutEnd = null;
            auth.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        /// <summary>
        /// Second half of an internal user's verification: confirms the email, sets a fresh temporary
        /// password (only its hash is stored) and emails it. Runs once per verification link, because the
        /// token is consumed before this and an already-verified account is refused.
        /// Data is true when the temporary password was emailed.
        /// </summary>
        private async Task<ServiceResult<bool>> CompleteInternalUserVerificationAsync(UserAuth auth)
        {
            // Normally unreachable (no live link exists once verified), but never re-issue credentials.
            if (auth.EmailConfirmed)
            {
                return ServiceResult<bool>.Failure("This email address has already been verified. Please sign in.");
            }

            if (!auth.IsActive)
            {
                return ServiceResult<bool>.Failure("This account is disabled. Contact your administrator.");
            }

            var fullName = (await userAuthRepository.GetInternalUserAccountAsync(auth.Id))?.InternalUser?.FullName ?? auth.Email;
            var temporaryPassword = SecurityTokenHelper.GenerateTemporaryPassword();

            auth.PasswordHash = passwordHasher.Hash(temporaryPassword);
            auth.EmailConfirmed = true;
            auth.MustChangePassword = true;
            auth.AccessFailedCount = 0;
            auth.LockoutEnd = null;
            auth.UpdatedAt = DateTimeOffset.UtcNow;
            try
            {
                await unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Two clicks raced on the same account (RowVersion); the one that saved first sends the email.
                return ServiceResult<bool>.Failure("This verification link is invalid or has expired.");
            }

            try
            {
                var baseUrl = (configuration["AppSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
                await emailService.SendTemplatedAsync(auth.Email, EmailTemplateType.InternalUserCredentials,
                    new Dictionary<string, string>
                    {
                        ["FullName"] = fullName,
                        ["Email"] = auth.Email,
                        ["TemporaryPassword"] = temporaryPassword,
                        ["LoginUrl"] = $"{baseUrl}/Auth/Login"
                    });
            }
            catch (Exception ex)
            {
                // The password itself is never logged.
                logger.LogError(ex, "Failed to email the temporary password to internal user {UserAuthId}; the account was returned to unverified.", auth.Id);

                // Nobody received this password, so it must not stay usable, and the account goes back to
                // unverified: a new verification link (resend on the sign-in page, or by the super admin)
                // repeats the whole step with a new temporary password.
                auth.PasswordHash = passwordHasher.Hash(SecurityTokenHelper.GenerateUrlSafeToken());
                auth.EmailConfirmed = false;
                auth.UpdatedAt = DateTimeOffset.UtcNow;
                await unitOfWork.SaveChangesAsync();

                return ServiceResult<bool>.Failure(
                    "We could not send your login details. Please request a new verification link, or contact your administrator.");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// Creates a verification token and emails the link. Failures are logged, not thrown,
        /// so a mail outage never blocks sign-up. Returns false if the email could not be sent.
        /// </summary>
        private async Task<bool> TrySendVerificationEmailAsync(UserAuth auth)
        {
            try
            {
                var token = await otpService.CreateEmailVerificationTokenAsync(auth.Id);
                var baseUrl = (configuration["AppSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
                var link = $"{baseUrl}/Auth/VerifyEmail?token={Uri.EscapeDataString(token)}";

                await emailService.SendTemplatedAsync(auth.Email, EmailTemplateType.EmailVerification,
                    new Dictionary<string, string>
                    {
                        ["ConsumerName"] = GetDisplayName(auth),
                        ["VerificationLink"] = link,
                        ["ExpiryDuration"] = $"{otpService.EmailVerificationExpiryHours} hours"
                    });
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send verification email to user {UserAuthId}.", auth.Id);
                return false;
            }
        }

        /// <summary>Name for the email greeting, taken from whichever profile the account has.</summary>
        private static string GetDisplayName(UserAuth auth)
        {
            var name = auth.InternalUser?.FullName
                ?? auth.Partner?.Name
                ?? auth.Customer?.PrimaryContactFullName
                ?? auth.Customer?.CompanyName;
            return string.IsNullOrWhiteSpace(name) ? "there" : name;
        }

        /// <summary>Support contact shown in emails; a generic line until a real contact is configured.</summary>
        private string GetSupportContact()
        {
            var contact = configuration["AppSettings:SupportContact"];
            return string.IsNullOrWhiteSpace(contact) ? "the PCI Pal team" : contact;
        }

        /// <summary>Maps the UserAuth + its profile to the DTO, or null if the profile is missing.</summary>
        private static AuthenticatedUserDto? BuildUserDto(UserAuth auth)
        {
            var dto = new AuthenticatedUserDto
            {
                UserId = auth.Id,
                Email = auth.Email,
                UserType = auth.UserType,
                MustChangePassword = auth.MustChangePassword
            };

            switch (auth.UserType)
            {
                case UserType.SuperAdmin or UserType.InternalUser when auth.InternalUser is not null:
                    dto.DisplayName = auth.InternalUser.FullName;
                    return dto;

                case UserType.Partner when auth.Partner is not null:
                    dto.DisplayName = auth.Partner.Name;
                    dto.PartnerId = auth.Partner.Id;
                    return dto;

                case UserType.Customer when auth.Customer is not null:
                    // Direct customers have no company name until they complete their profile.
                    dto.DisplayName = auth.Customer.CompanyName ?? auth.Email;
                    dto.PartnerId = auth.Customer.PartnerId;
                    dto.CustomerId = auth.Customer.Id;
                    return dto;

                default:
                    return null;
            }
        }
    }
}

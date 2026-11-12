using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Auth;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>Internal (staff) accounts, managed by admins.</summary>
    public class InternalUserService(
        IUserAuthRepository userAuthRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IAuthService authService) : IInternalUserService
    {
        // InternalUsersController matches on these texts.
        private const string InternalUserNotFound = "Internal user not found.";
        private const string VerifiedUserNotEditable = "This user has already verified their email, so their name and email can no longer be changed.";

        public async Task<IReadOnlyList<InternalUserSummaryDto>> GetAllAsync()
        {
            var accounts = await userAuthRepository.GetInternalAccountsAsync();
            return accounts
                .Select(a => new InternalUserSummaryDto
                {
                    Id = a.Id,
                    FullName = a.InternalUser?.FullName ?? a.Email,
                    Email = a.Email,
                    IsActive = a.IsActive,
                    EmailConfirmed = a.EmailConfirmed,
                    IsSuperAdmin = a.UserType == UserType.SuperAdmin,
                    CreatedAt = a.CreatedAt
                })
                .ToList();
        }

        public async Task<InternalUserSummaryDto?> GetAsync(Guid id)
        {
            var auth = await userAuthRepository.GetInternalUserAccountAsync(id);
            return auth is null
                ? null
                : new InternalUserSummaryDto
                {
                    Id = auth.Id,
                    FullName = auth.InternalUser!.FullName,
                    Email = auth.Email,
                    IsActive = auth.IsActive,
                    EmailConfirmed = auth.EmailConfirmed,
                    IsSuperAdmin = false,
                    CreatedAt = auth.CreatedAt
                };
        }

        public async Task<ServiceResult<bool>> UpdateAsync(Guid id, string fullName, string email)
        {
            var auth = await userAuthRepository.GetInternalUserAccountAsync(id);
            if (auth is null)
            {
                return ServiceResult<bool>.Failure(InternalUserNotFound);
            }

            if (auth.EmailConfirmed)
            {
                return ServiceResult<bool>.Failure(VerifiedUserNotEditable);
            }

            var normalizedEmail = email.NormalizeEmail();
            if (normalizedEmail != auth.NormalizedEmail
                && await userAuthRepository.GetByNormalizedEmailAsync(normalizedEmail) is not null)
            {
                return ServiceResult<bool>.Failure("Email is not available.");
            }

            auth.InternalUser!.FullName = fullName.Trim();
            auth.Email = email.Trim();
            auth.NormalizedEmail = normalizedEmail;
            auth.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            // A fresh link goes to the address now on the account; earlier links (possibly sent to an old
            // address) are invalidated, so only the latest address can verify and receive the temporary password.
            // An inactive user gets no link (SendVerificationEmailAsync refuses), as with "Resend verification".
            var emailed = auth.IsActive && (await authService.SendVerificationEmailAsync(auth.Id)).Succeeded;
            return ServiceResult<bool>.Success(emailed);
        }

        public async Task<ServiceResult> SetActiveAsync(Guid id, bool isActive)
        {
            var auth = await userAuthRepository.GetInternalUserAccountAsync(id);
            if (auth is null)
            {
                return ServiceResult.Failure(InternalUserNotFound);
            }

            auth.IsActive = isActive;
            auth.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<bool>> CreateAsync(CreateInternalUserDto dto)
        {
            var normalizedEmail = dto.Email.NormalizeEmail();

            // Sign-in looks accounts up by email alone, so an email must be unique across all account types.
            if (await userAuthRepository.GetByNormalizedEmailAsync(normalizedEmail) is not null)
            {
                return ServiceResult<bool>.Failure("Email is not available.");
            }

            var auth = new UserAuth
            {
                Email = dto.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                // A random password nobody knows; the temporary one is generated and emailed once the
                // user verifies their address (AuthService.VerifyEmailAsync). Until then sign-in is refused.
                PasswordHash = passwordHasher.Hash(SecurityTokenHelper.GenerateUrlSafeToken()),
                UserType = UserType.InternalUser,
                EmailConfirmed = false,
                MustChangePassword = true,
                InternalUser = new InternalUser { FullName = dto.FullName.Trim() }
            };
            await userAuthRepository.AddAsync(auth);
            await unitOfWork.SaveChangesAsync();

            // The account exists even if the email fails; the super admin can re-send the link.
            var emailed = await authService.SendVerificationEmailAsync(auth.Id);
            return ServiceResult<bool>.Success(emailed.Succeeded);
        }

        public async Task<ServiceResult> ResendVerificationAsync(Guid id)
        {
            var auth = await userAuthRepository.GetInternalUserAccountAsync(id);
            if (auth is null)
            {
                return ServiceResult.Failure(InternalUserNotFound);
            }

            if (auth.EmailConfirmed)
            {
                return ServiceResult.Failure("This user has already verified their email.");
            }

            if (!auth.IsActive)
            {
                return ServiceResult.Failure("Activate this user before re-sending the verification email.");
            }

            return await authService.SendVerificationEmailAsync(auth.Id);
        }
    }
}

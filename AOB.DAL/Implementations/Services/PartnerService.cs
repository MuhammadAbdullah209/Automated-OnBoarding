using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Auth;
using AOB.BAL.DTOs.Partners;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>Partner information and creation for internal users.</summary>
    public class PartnerService(
        IPartnerRepository partnerRepository,
        IUserAuthRepository userAuthRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<PartnerService> logger) : IPartnerService
    {
        public async Task<IReadOnlyList<PartnerSummaryDto>> GetAllAsync()
            => await partnerRepository.GetAllSummariesAsync();

        public async Task<PartnerSummaryDto?> GetAsync(Guid id)
            => await partnerRepository.GetSummaryAsync(id);

        public async Task<ServiceResult> CreateAsync(CreateAccountDto dto)
        {
            var normalizedEmail = dto.Email.NormalizeEmail();

            // Sign-in looks accounts up by email alone, so an email must be unique across all account types.
            if (await userAuthRepository.GetByNormalizedEmailAsync(normalizedEmail) is not null)
            {
                return ServiceResult.Failure("Email is not available.");
            }

            await userAuthRepository.AddAsync(new UserAuth
            {
                Email = dto.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = passwordHasher.Hash(dto.Password),
                UserType = UserType.Partner,
                // The admin vouches for the address and hands over the initial password.
                EmailConfirmed = true,
                // The admin knows this password, so the partner must replace it at first sign-in.
                MustChangePassword = true,
                Partner = new Partner { Name = dto.Name.Trim() }
            });
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("SecurityEvent PartnerAccountCreated Email={Email}", normalizedEmail);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateAsync(Guid id, UpdatePartnerDto dto)
        {
            var partner = await partnerRepository.GetWithAuthAsync(id);
            if (partner is null)
            {
                return ServiceResult.Failure("Partner not found.");
            }

            var normalizedEmail = dto.Email.NormalizeEmail();
            if (normalizedEmail != partner.Auth.NormalizedEmail
                && await userAuthRepository.GetByNormalizedEmailAsync(normalizedEmail) is not null)
            {
                return ServiceResult.Failure("Email is not available.");
            }

            if (normalizedEmail != partner.Auth.NormalizedEmail)
            {
                logger.LogWarning("SecurityEvent PartnerLoginEmailChanged PartnerId={PartnerId} From={From} To={To}",
                    id, partner.Auth.NormalizedEmail, normalizedEmail);
            }

            var now = DateTimeOffset.UtcNow;
            partner.Name = dto.Name.Trim();
            partner.UpdatedAt = now;
            partner.Auth.Email = dto.Email.Trim();
            partner.Auth.NormalizedEmail = normalizedEmail;
            partner.Auth.UpdatedAt = now;
            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> SetActiveAsync(Guid id, bool isActive)
        {
            if (!isActive && id == PartnerConstants.PcipalPartnerId)
            {
                return ServiceResult.Failure("PCIPAL owns all direct customers and cannot be deactivated.");
            }

            var partner = await partnerRepository.GetWithAuthAsync(id);
            if (partner is null)
            {
                return ServiceResult.Failure("Partner not found.");
            }

            var now = DateTimeOffset.UtcNow;
            partner.IsActive = isActive;
            partner.UpdatedAt = now;
            partner.Auth.IsActive = isActive;
            partner.Auth.UpdatedAt = now;
            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
    }
}

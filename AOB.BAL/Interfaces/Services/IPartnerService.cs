using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Auth;
using AOB.BAL.DTOs.Partners;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Partner information for internal users.</summary>
    public interface IPartnerService
    {
        /// <summary>Lists every partner with its customer count.</summary>
        Task<IReadOnlyList<PartnerSummaryDto>> GetAllAsync();

        /// <summary>One partner with its customer count, or null when it does not exist.</summary>
        Task<PartnerSummaryDto?> GetAsync(Guid id);

        /// <summary>Creates a partner and its login (Name = organisation name).</summary>
        Task<ServiceResult> CreateAsync(CreateAccountDto dto);

        /// <summary>Changes the partner's name and login email (the email must stay unique across all accounts).</summary>
        Task<ServiceResult> UpdateAsync(Guid id, UpdatePartnerDto dto);

        /// <summary>
        /// Activates or deactivates a partner. Its login follows, so an inactive partner cannot sign in.
        /// PCIPAL (owner of all direct customers) cannot be deactivated.
        /// </summary>
        Task<ServiceResult> SetActiveAsync(Guid id, bool isActive);
    }
}

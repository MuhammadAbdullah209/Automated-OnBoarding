using AOB.BAL.DTOs.Partners;
using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Partner organisation data access.</summary>
    public interface IPartnerRepository
    {
        Task<Partner?> GetByIdAsync(Guid id);

        /// <summary>The partner with its login loaded (tracked, so changes can be saved), or null.</summary>
        Task<Partner?> GetWithAuthAsync(Guid id);

        /// <summary>One partner with its login email and customer count, or null.</summary>
        Task<PartnerSummaryDto?> GetSummaryAsync(Guid id);

        /// <summary>Every partner with its login email and customer count, by name.</summary>
        Task<IReadOnlyList<PartnerSummaryDto>> GetAllSummariesAsync();
    }
}

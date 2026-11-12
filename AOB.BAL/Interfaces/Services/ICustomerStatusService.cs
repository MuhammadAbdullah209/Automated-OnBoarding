using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Derives and updates customer Pending / Onboarded status; handles Disabled.</summary>
    public interface ICustomerStatusService
    {
        /// <summary>Recomputes Pending vs Onboarded unless the customer is Disabled.</summary>
        Task RefreshDerivedStatusAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<ServiceResult> SetDisabledAsync(Guid customerId, bool disabled, CancellationToken cancellationToken = default);
    }
}

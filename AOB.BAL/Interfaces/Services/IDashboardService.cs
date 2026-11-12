using AOB.BAL.DTOs.Dashboard;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Dashboard metrics scoped by user type.</summary>
    public interface IDashboardService
    {
        Task<DashboardDto> GetInternalDashboardAsync(CancellationToken cancellationToken = default);

        Task<DashboardDto> GetPartnerDashboardAsync(Guid partnerId, CancellationToken cancellationToken = default);

        Task<DashboardDto> GetCustomerDashboardAsync(Guid customerId, CancellationToken cancellationToken = default);
    }
}

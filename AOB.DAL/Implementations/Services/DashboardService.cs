using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Dashboard;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;

namespace AOB.DAL.Implementations.Services
{
    public class DashboardService(
        ICustomerRepository customerRepository,
        IProjectRepository projectRepository) : IDashboardService
    {
        private const int RecentProjectLimit = 8;

        public async Task<DashboardDto> GetInternalDashboardAsync(CancellationToken cancellationToken = default)
        {
            var segments = await customerRepository.GetProjectSegmentCountsAsync(cancellationToken: cancellationToken);
            var projectCounts = await projectRepository.CountByStatusAsync(cancellationToken: cancellationToken);
            var totalProjects = await projectRepository.CountAsync(cancellationToken: cancellationToken);
            var recent = await projectRepository.GetRecentAsync(RecentProjectLimit, cancellationToken: cancellationToken);
            var info = await projectRepository.CountInformationRequestsAsync(cancellationToken: cancellationToken);

            return Build(segments, totalProjects, projectCounts, recent, info);
        }

        public async Task<DashboardDto> GetPartnerDashboardAsync(Guid partnerId, CancellationToken cancellationToken = default)
        {
            var segments = await customerRepository.GetProjectSegmentCountsAsync(partnerId, cancellationToken: cancellationToken);
            var projectCounts = await projectRepository.CountByStatusAsync(partnerId, cancellationToken: cancellationToken);
            var totalProjects = await projectRepository.CountAsync(partnerId, cancellationToken: cancellationToken);
            var recent = await projectRepository.GetRecentAsync(RecentProjectLimit, partnerId, cancellationToken: cancellationToken);
            var info = await projectRepository.CountInformationRequestsAsync(partnerId, cancellationToken: cancellationToken);

            return Build(segments, totalProjects, projectCounts, recent, info);
        }

        public async Task<DashboardDto> GetCustomerDashboardAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            var segments = await customerRepository.GetProjectSegmentCountsAsync(customerId: customerId, cancellationToken: cancellationToken);
            var projectCounts = await projectRepository.CountByStatusAsync(customerId: customerId, cancellationToken: cancellationToken);
            var totalProjects = await projectRepository.CountAsync(customerId: customerId, cancellationToken: cancellationToken);
            var recent = await projectRepository.GetRecentAsync(RecentProjectLimit, customerId: customerId, cancellationToken: cancellationToken);
            var info = await projectRepository.CountInformationRequestsAsync(customerId: customerId, cancellationToken: cancellationToken);

            return Build(segments, totalProjects, projectCounts, recent, info);
        }

        private static DashboardDto Build(
            CustomerProjectSegmentCountsDto segments,
            int totalProjects,
            IReadOnlyDictionary<ProjectStatus, int> projectCounts,
            IReadOnlyList<DashboardRecentProjectDto> recent,
            (int Requested, int Submitted) info)
            => new()
            {
                TotalCustomers = segments.TotalCustomers,
                TotalProjects = totalProjects,
                CustomerSegments = segments,
                InformationRequested = info.Requested,
                InformationSubmitted = info.Submitted,
                ProjectCountsByStatus = projectCounts,
                RecentProjects = recent
            };
    }
}

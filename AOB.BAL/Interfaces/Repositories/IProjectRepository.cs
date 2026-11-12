using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Dashboard;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Project (onboarding case) data access.</summary>
    public interface IProjectRepository
    {
        /// <summary>All of a customer's projects, newest first.</summary>
        Task<IReadOnlyList<Project>> GetByCustomerAsync(Guid customerId);

        /// <summary>Project statuses grouped by customer id (for list screens).</summary>
        Task<IReadOnlyDictionary<Guid, IReadOnlyList<ProjectStatus>>> GetProjectStatusesByCustomerIdsAsync(
            IReadOnlyList<Guid> customerIds);

        /// <summary>Filtered, paged projects for a customer (newest first). TotalCount is before Skip/Take.</summary>
        Task<(IReadOnlyList<Project> Items, int TotalCount)> ListAsync(Guid customerId, ProjectListQuery query);

        /// <summary>Filtered, paged projects across all customers (internal staff). Includes Customer for display.</summary>
        Task<(IReadOnlyList<Project> Items, int TotalCount)> ListAllAsync(ProjectListQuery query);

        /// <summary>
        /// One of the customer's projects with all its onboarding screens loaded (tracked, so changes
        /// can be saved). Null if it does not exist or belongs to another customer.
        /// </summary>
        Task<Project?> GetWithConfigurationAsync(Guid customerId, Guid projectId);

        /// <summary>The customer's project without its screens loaded (tracked). Null if missing or not theirs.</summary>
        Task<Project?> GetForCustomerAsync(Guid customerId, Guid projectId);

        /// <summary>Project by id (tracked). Null if missing.</summary>
        Task<Project?> GetByIdAsync(Guid projectId);

        /// <summary>True if the customer already has a project with this name.</summary>
        Task<bool> NameExistsAsync(Guid customerId, string name);

        /// <summary>Adds the project. Saved by IUnitOfWork.SaveChangesAsync.</summary>
        Task AddAsync(Project project);

        /// <summary>True if the project already has a ProjectProvisioningDetails row.</summary>
        Task<bool> ProvisioningDetailExistsAsync(Guid projectId);

        /// <summary>The project's ProjectProvisioningDetails row (not tracked), or null if there is none.</summary>
        Task<ProjectProvisioningDetail?> GetProvisioningDetailAsync(Guid projectId);

        /// <summary>Adds the project's provisioning details. Saved by IUnitOfWork.SaveChangesAsync.</summary>
        Task AddProvisioningDetailAsync(ProjectProvisioningDetail detail);

        /// <summary>The project's ProjectProvisioningDetails row, tracked so it can be updated; null if there is none.</summary>
        Task<ProjectProvisioningDetail?> GetProvisioningDetailForUpdateAsync(Guid projectId);

        /// <summary>Project count for all customers, one partner's customers, or a single customer.</summary>
        Task<int> CountAsync(Guid? partnerId = null, Guid? customerId = null, CancellationToken cancellationToken = default);

        /// <summary>How many users the customer has uploaded for the project (the users CSV).</summary>
        Task<int> CountCustomerUsersAsync(Guid projectId);

        /// <summary>True when the project has an information request that is not fully approved yet.</summary>
        Task<bool> HasOpenInformationRequestAsync(Guid projectId);

        /// <summary>How many projects have an information request waiting for the customer / waiting for review.</summary>
        Task<(int Requested, int Submitted)> CountInformationRequestsAsync(
            Guid? partnerId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default);

        /// <summary>Projects grouped by status with the same scope as <see cref="CountAsync"/>.</summary>
        Task<IReadOnlyDictionary<ProjectStatus, int>> CountByStatusAsync(
            Guid? partnerId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default);

        /// <summary>Newest projects in scope, with customer company name when not single-customer scope.</summary>
        Task<IReadOnlyList<DashboardRecentProjectDto>> GetRecentAsync(
            int take,
            Guid? partnerId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default);
    }
}

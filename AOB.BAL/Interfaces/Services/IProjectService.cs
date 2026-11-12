using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>A customer's onboarding projects.</summary>
    public interface IProjectService
    {
        /// <summary>Filtered, paged project summaries for the customer's list screen.</summary>
        Task<PagedResult<ProjectSummaryDto>> GetProjectsPagedAsync(Guid customerId, ProjectListQuery query);

        /// <summary>Filtered, paged project summaries across all customers (internal staff).</summary>
        Task<PagedResult<ProjectSummaryDto>> GetAllProjectsPagedAsync(ProjectListQuery query);

        /// <summary>A project and how far its steps have got. Fails if it is not the customer's project.</summary>
        Task<ServiceResult<ProjectOverviewDto>> GetOverviewAsync(Guid customerId, Guid projectId);

        /// <summary>
        /// Submits a project once all four steps are saved. The domain build rules then decide: a standard
        /// build moves to Provisioning and provisions itself (then account creation, where needed, and Completed);
        /// a custom build waits in Pending approval. The customer is not told which. A submitted project can no longer be edited.
        /// </summary>
        Task<ServiceResult<ProjectSubmitResultDto>> SubmitAsync(Guid customerId, Guid projectId);

        /// <summary>Saves a completed project as draft instead of submitting.</summary>
        Task<ServiceResult> SaveAsDraftAsync(Guid customerId, Guid projectId);

        /// <summary>
        /// Internal workflow: Submitted moves to Pending approval, and Pending approval (a custom build that has
        /// been approved) moves to Provisioning, which then runs by itself. A project stuck in Provisioning is run again.
        /// </summary>
        Task<ServiceResult> AdvanceInternalWorkflowAsync(Guid projectId);

        /// <summary>
        /// Custom build, internal workflow: the account was created for the customer's users (uploaded as a CSV).
        /// Needs the project in Account creation and at least one user. It stays in Account creation until marked completed.
        /// </summary>
        Task<ServiceResult> MarkAccountCreatedAsync(Guid projectId);

        /// <summary>
        /// Custom build, internal workflow: the project is done. Allowed after the account was created, or straight
        /// from Provisioning when no account is needed (the payment page is embedded in the CRM).
        /// </summary>
        Task<ServiceResult> MarkCompletedAsync(Guid projectId);

        /// <summary>What the provisioning page shows; fails unless provisioning has begun for the project.</summary>
        Task<ServiceResult<ProvisioningInfoDto>> GetProvisioningInfoAsync(Guid customerId, Guid projectId);

        /// <summary>
        /// The Setup Documentation Pack (SDP) of a project. Fails unless the project is Completed and all its
        /// provisioning steps have run.
        /// </summary>
        Task<ServiceResult<SetupDocumentDto>> GetSetupDocumentAsync(Guid customerId, Guid projectId);

        /// <summary>
        /// Creates a project in Draft at the Discovery step. Needs a unique name for the customer
        /// Creates a project in progress at the Discovery step. Needs a unique name for the customer
        /// and at least one available service.
        /// </summary>
        Task<ServiceResult<Guid>> CreateAsync(Guid customerId, CreateProjectDto dto);

        /// <summary>How long a project can still be restored after deletion is confirmed (at least 1 minute).</summary>
        int GetDeletionGracePeriodMinutes();

        /// <summary>
        /// Starts the deletion countdown (ProjectDeletion:GracePeriodMinutes, default 5) and stores
        /// <paramref name="reason"/>. Nothing is deleted yet: the project can be restored until the time
        /// returned, after which a background job removes it. Asking again does not extend the countdown.
        /// </summary>
        Task<ServiceResult<DateTimeOffset>> ScheduleDeletionAsync(Guid customerId, Guid projectId, string reason);

        /// <summary>Cancels a pending deletion.</summary>
        Task<ServiceResult> RestoreAsync(Guid customerId, Guid projectId);

        /// <summary>True while the project is waiting to be deleted.</summary>
        Task<bool> IsPendingDeletionAsync(Guid customerId, Guid projectId);
    }
}

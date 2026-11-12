using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>
    /// Loads and saves the four onboarding screens of a project. Every method takes the customer id
    /// so a customer can only touch their own project; saving fails once a project is submitted.
    /// </summary>
    public interface IProjectConfigurationService
    {
        /// <summary>Everything entered across all four screens, for the read-only "All details" page.</summary>
        Task<ServiceResult<ProjectFullDetailsDto>> GetFullDetailsAsync(Guid customerId, Guid projectId);

        Task<ServiceResult<ProjectDiscoveryDto>> GetDiscoveryAsync(Guid customerId, Guid projectId);
        Task<ServiceResult> SaveDiscoveryAsync(Guid customerId, Guid projectId, ProjectDiscoveryDto dto);

        Task<ServiceResult<ProjectTechnicalEnvironmentDto>> GetTechnicalEnvironmentAsync(Guid customerId, Guid projectId);
        Task<ServiceResult> SaveTechnicalEnvironmentAsync(Guid customerId, Guid projectId, ProjectTechnicalEnvironmentDto dto);

        Task<ServiceResult<ProjectPaymentConfigurationDto>> GetPaymentConfigurationAsync(Guid customerId, Guid projectId);
        Task<ServiceResult> SavePaymentConfigurationAsync(Guid customerId, Guid projectId, ProjectPaymentConfigurationDto dto);

        /// <summary>
        /// The fields of the payment gateway chosen on the Payment Configuration step. Fails if no gateway has
        /// been chosen yet.
        /// </summary>
        Task<ServiceResult<GatewayFieldOptionsDto>> GetGatewayFieldOptionsAsync(Guid customerId, Guid projectId);

        Task<ServiceResult<ProjectGatewayConfigurationDto>> GetGatewayConfigurationAsync(Guid customerId, Guid projectId);
        Task<ServiceResult> SaveGatewayConfigurationAsync(Guid customerId, Guid projectId, ProjectGatewayConfigurationDto dto);

        /// <summary>
        /// Saves only the redirect URL of an Adaptive Configuration that already uses Redirect. Used from the project
        /// overview when the URL is still missing. Fails once the project has been submitted.
        /// </summary>
        Task<ServiceResult> SaveRedirectUrlAsync(Guid customerId, Guid projectId, string? redirectUrl);
    }
}

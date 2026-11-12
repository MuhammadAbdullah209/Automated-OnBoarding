using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Launches a PCI Pal flow session for a submitted project.</summary>
    public interface IFlowLaunchService
    {
        /// <summary>
        /// Gets a token with the project's ProjectProvisioningDetails, then launches the flow and
        /// returns what the browser needs to show it in an iframe. Fails if the project is not the
        /// customer's, has no provisioning details, or either API call fails.
        /// </summary>
        Task<ServiceResult<FlowLaunchDto>> LaunchAsync(Guid customerId, Guid projectId);
    }
}

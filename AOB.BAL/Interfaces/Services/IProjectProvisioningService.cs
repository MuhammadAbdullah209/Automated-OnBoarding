using AOB.BAL.Common.Models;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Runs the provisioning steps of a project (provisioning is one status, made of several steps).</summary>
    public interface IProjectProvisioningService
    {
        /// <summary>
        /// Runs every provisioning step in order for a project that is in Provisioning. The project stays in
        /// Provisioning. If a step fails it can be run again; steps that already finished are not repeated.
        /// </summary>
        Task<ServiceResult> RunAsync(Guid projectId);
    }
}

using AOB.BAL.Entities;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// One provisioning step. Each step fills in its part of the project's ProjectProvisioningDetails row.
    /// Steps run in registration order (see DependencyInjection) and are safe to run again.
    /// </summary>
    public interface IProvisioningStep
    {
        string Name { get; }

        /// <summary>Whether this step's part of the details is already filled in.</summary>
        bool IsDone(ProjectProvisioningDetail detail);

        Task ExecuteAsync(ProjectProvisioningDetail detail);
    }

    // The values are fixed for now: every project is provisioned on the same platform and tenant.

    /// <summary>Step 1: the platform, tenant and region the project is provisioned on.</summary>
    public sealed class PlatformAndTenantStep : IProvisioningStep
    {
        public string Name => "Platform and tenant";

        public bool IsDone(ProjectProvisioningDetail d)
            => !string.IsNullOrWhiteSpace(d.TenantName) && !string.IsNullOrWhiteSpace(d.TenantId) && !string.IsNullOrWhiteSpace(d.Region);

        public Task ExecuteAsync(ProjectProvisioningDetail d)
        {
            d.Platform = "pcipalstaging";
            d.TenantName = "Sami Borderland";
            d.TenantId = "768";
            d.Region = "euwest1";
            return Task.CompletedTask;
        }
    }

    /// <summary>Step 2: the credentials used to sign in to the platform.</summary>
    public sealed class ServiceCredentialsStep : IProvisioningStep
    {
        public string Name => "Service credentials";

        public bool IsDone(ProjectProvisioningDetail d)
            => !string.IsNullOrWhiteSpace(d.Username) && !string.IsNullOrWhiteSpace(d.ClientId) && !string.IsNullOrWhiteSpace(d.ClientSecret);

        public Task ExecuteAsync(ProjectProvisioningDetail d)
        {
            d.Username = "onboarding-user";
            d.ClientId = "onboarding-application";
            d.ClientSecret = "RLU0bREIs9vF0hiUUtyvrJOQUfmVOfulac3ROR9i";
            return Task.CompletedTask;
        }
    }

    /// <summary>Step 3: the flow that is launched for the project.</summary>
    public sealed class FlowStep : IProvisioningStep
    {
        public string Name => "Flow";

        public bool IsDone(ProjectProvisioningDetail d) => !string.IsNullOrWhiteSpace(d.FlowId);

        public Task ExecuteAsync(ProjectProvisioningDetail d)
        {
            d.FlowId = "5840";
            return Task.CompletedTask;
        }
    }
}

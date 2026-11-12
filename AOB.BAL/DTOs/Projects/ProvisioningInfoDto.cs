namespace AOB.BAL.DTOs.Projects
{
    /// <summary>What the provisioning page shows about a project that is in Provisioning.</summary>
    public class ProvisioningInfoDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }

        /// <summary>The platform the project is provisioned on, e.g. pcipalstaging.</summary>
        public string? Platform { get; set; }

        /// <summary>Where the project is: Completed or Account creation means provisioning has finished.</summary>
        public AOB.BAL.Common.Enums.ProjectStatus Status { get; set; }

        /// <summary>All the provisioning steps have run (the provisioning details are filled in).</summary>
        public bool StepsComplete { get; set; }

        /// <summary>An internal user marked the account created.</summary>
        public bool AccountCreated { get; set; }
    }
}

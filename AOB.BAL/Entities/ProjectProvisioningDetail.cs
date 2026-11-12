using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// Provisioning details for a submitted project (1:1 with Project), stored in the
    /// ProjectProvisioningDetails table. The row is created when the project is submitted; the
    /// credentials and ids (client id/secret, tenant id, flow id) are filled in once provisioned.
    /// </summary>
    public class ProjectProvisioningDetail
    {
        /// <summary>Platform every project is provisioned on for now.</summary>
        public const string DefaultPlatform = "pcipalstaging";

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        [Required, MaxLength(100)]
        public string Platform { get; set; } = DefaultPlatform;

        [MaxLength(200)]
        public string? TenantName { get; set; }

        [MaxLength(320)]
        public string? Username { get; set; }

        [MaxLength(200)]
        public string? ClientId { get; set; }

        [MaxLength(500)]
        public string? ClientSecret { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        [MaxLength(200)]
        public string? TenantId { get; set; }

        [MaxLength(200)]
        public string? FlowId { get; set; }

        /// <summary>When the account was created. Only set for a project whose payment page is hosted by PCI Pal.</summary>
        public DateTimeOffset? AccountCreatedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public Project Project { get; set; } = null!;
    }
}

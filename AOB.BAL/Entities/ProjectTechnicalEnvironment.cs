using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// Answers from the "Technical Environment" screen (1:1 with Project). The business system / CRM
    /// itself is Project.CrmSystem and Project.CrmPlatform.
    /// </summary>
    public class ProjectTechnicalEnvironment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        // "Regions where payments are processed." Comma-separated PaymentRegion names.
        [MaxLength(100)]
        public string? PaymentRegions { get; set; }

        // "Staging Regions." Comma-separated PciPalRegion names.
        [MaxLength(300)]
        public string? StagingRegions { get; set; }

        // "Production Regions." Comma-separated PciPalRegion names.
        [MaxLength(300)]
        public string? ProductionRegions { get; set; }

        // "Can your systems reach external service endpoints on the internet?"
        public OutboundAccess? OutboundAccess { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public Project Project { get; set; } = null!;
    }
}

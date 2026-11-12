using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>Answers from the "Business &amp; Use-Case Discovery" screen (1:1 with Project).</summary>
    public class ProjectDiscovery
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        // "How do your customers currently make payments?" Comma-separated CurrentPaymentMethod names.
        [MaxLength(300)]
        public string? CurrentPaymentMethods { get; set; }

        // Free text describing the payment journey (used when "Another way" is chosen).
        [MaxLength(2000)]
        public string? PaymentJourneyDescription { get; set; }

        // "How would you like the payment experience to work in your CRM?"
        public CrmPaymentExperience? CrmPaymentExperience { get; set; }

        // Portal URLs where the payment form is embedded (only for EmbeddedIframe): one URL per line.
        [MaxLength(4000)]
        public string? PortalUrls { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public Project Project { get; set; } = null!;
    }
}

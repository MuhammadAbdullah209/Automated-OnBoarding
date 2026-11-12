using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// Gateway configuration from the "Adaptive Configuration" screen (1:1 with Project): how payment
    /// results come back to the customer's system, and the contact centre details. The field
    /// fields (authentication / inputs / outputs) are rows in ProjectGatewayFields.
    /// </summary>
    public class ProjectGatewayConfiguration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        // "Post-Transactional Data Transfer." Comma-separated PostTransactionMethod names.
        [MaxLength(200)]
        public string? PostTransactionMethods { get; set; }

        // Where customers are sent after paying. Only used (and required) when PostTransactionMethods
        // contains Redirect.
        [MaxLength(500)]
        public string? RedirectUrl { get; set; }

        // Contact centre details. Only asked when the customer takes phone payments with an agent
        // (ProjectDiscovery.CurrentPaymentMethods contains PhoneCallWithAgent), so all three are optional.
        public ContactCentrePlatform? ContactCentrePlatform { get; set; }

        public ContactCentreConnectionType? ConnectionType { get; set; }

        // "Call and payment handover notes."
        [MaxLength(2000)]
        public string? HandoverNotes { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public Project Project { get; set; } = null!;
    }
}

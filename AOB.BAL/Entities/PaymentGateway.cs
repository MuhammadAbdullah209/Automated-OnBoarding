using System.ComponentModel.DataAnnotations;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// A payment gateway / PSP that PCI Pal integrates with (e.g. Stripe), with the data fields
    /// that gateway exchanges (see PaymentGatewayField).
    /// </summary>
    public class PaymentGateway
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// True when this gateway is supported as part of PCI Pal's standard build. Choosing a gateway that is
        /// not standard makes the project a custom build.
        /// </summary>
        public bool IsStandard { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public ICollection<PaymentGatewayField> Fields { get; set; } = new List<PaymentGatewayField>();
    }
}

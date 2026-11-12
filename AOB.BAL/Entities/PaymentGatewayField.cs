using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// A data field a payment gateway exchanges with PCI Pal, e.g. "Transaction Amount" (input) or
    /// "Authorisation Code" (output). Many fields per gateway.
    /// </summary>
    public class PaymentGatewayField
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(PaymentGateway))]
        public Guid PaymentGatewayId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Authentication = 0, Required = 1, Conditional = 2, Optional = 3.
        public GatewayFieldType FieldType { get; set; }

        // true = an input field (sent by the customer's system to PCI Pal);
        // false = an output field (returned to the customer's system).
        // Not meaningful for Authentication fields (stored as false).
        public bool IsInput { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// True when this field is part of the gateway's standard build. Selecting a field that is not
        /// standard makes the project a custom build.
        /// </summary>
        public bool IsStandard { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public PaymentGateway PaymentGateway { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// One gateway field included in a project's integration, from the "Adaptive Configuration"
    /// screen. A project has many rows (one per field). Only fields that are part of the
    /// integration are stored: the authentication fields, the required fields, and whichever
    /// optional fields the customer selected, all taken from the project's PaymentGateway
    /// (see PaymentGatewayField).
    ///
    /// Name, FieldType, IsInput and Description are copied from the gateway's field when the row is
    /// saved, so a submitted project keeps showing exactly what was submitted even if the gateway's
    /// field list is edited later.
    /// </summary>
    public class ProjectGatewayFields
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        // The gateway field this row came from. Null for rows saved before gateways were configured
        // in the database, and cleared if the gateway field is later deleted.
        public Guid? PaymentGatewayFieldId { get; set; }

        // Field name, e.g. "amount" or "Transaction Amount" (copied from the gateway field).
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Authentication = 0, Required = 1, Conditional = 2, Optional = 3.
        public GatewayFieldType FieldType { get; set; }

        // true = an input field (sent by the customer's system to PCI Pal);
        // false = an output field (returned to the customer's system).
        // Not meaningful for Authentication rows (stored as false).
        public bool IsInput { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public Project Project { get; set; } = null!;

        public PaymentGatewayField? PaymentGatewayField { get; set; }
    }
}

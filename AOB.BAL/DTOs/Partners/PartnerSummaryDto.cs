namespace AOB.BAL.DTOs.Partners
{
    /// <summary>One row of the internal Partners list.</summary>
    public class PartnerSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>The partner's login email.</summary>
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        /// <summary>Customers owned by this partner (direct customers count for PCIPAL).</summary>
        public int CustomerCount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}

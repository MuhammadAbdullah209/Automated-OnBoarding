using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Customers
{
    /// <summary>Lightweight customer row for list screens.</summary>
    public class CustomerSummaryDto
    {
        public Guid Id { get; set; }
        public string? CompanyName { get; set; }
        public string? PrimaryContactFullName { get; set; }
        public bool IsDirect { get; set; }
        public bool HasLogin { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public int ProjectCount { get; set; }
        public CustomerStatus Status { get; set; }

        public Guid PartnerId { get; set; }
        public string? PartnerName { get; set; }
    }
}

using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Documents;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.DTOs.Customers
{
    /// <summary>Read-only customer view for a partner (profile, documents, projects).</summary>
    public class CustomerPartnerDetailDto
    {
        public Guid Id { get; set; }
        public CustomerStatus Status { get; set; }
        /// <summary>The partner that owns this customer.</summary>
        public Guid PartnerId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsDirect { get; set; }
        public bool HasLogin { get; set; }
        public string? LoginEmail { get; set; }

        public CustomerProfileDto Profile { get; set; } = new();

        public IReadOnlyList<DocumentChecklistItemDto> Documents { get; set; } = Array.Empty<DocumentChecklistItemDto>();

        public IReadOnlyList<ProjectSummaryDto> Projects { get; set; } = Array.Empty<ProjectSummaryDto>();
    }
}

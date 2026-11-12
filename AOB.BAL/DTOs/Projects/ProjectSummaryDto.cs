using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>One row of a customer's project list.</summary>
    public class ProjectSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<PciPalService> Services { get; set; } = new();
        public ProjectStatus Status { get; set; }
        public ProjectStep CurrentStep { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>Set while this project is waiting to be deleted.</summary>
        public DateTimeOffset? DeletionScheduledFor { get; set; }

        /// <summary>The latest information request: Requested (customer to act) or Submitted (to review).</summary>
        public InformationRequestStatus? InformationRequestStatus { get; set; }

        /// <summary>In account creation, with the users CSV asked for and not uploaded yet: the customer has to act.</summary>
        public bool AwaitingUsersCsv { get; set; }

        /// <summary>The payment page is hosted by PCI Pal, so an account is created after provisioning.</summary>
        public bool RequiresAccount { get; set; }

        // Filled on the all-projects list only.
        public Guid CustomerId { get; set; }
        public string? CustomerName { get; set; }
    }
}

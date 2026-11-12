using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.DTOs.Dashboard
{
    /// <summary>Role-scoped dashboard metrics and recent activity.</summary>
    public class DashboardDto
    {
        public int TotalCustomers { get; set; }
        public int TotalProjects { get; set; }

        public CustomerProjectSegmentCountsDto CustomerSegments { get; set; } = new();

        /// <summary>Projects where staff asked for information and the customer has not answered yet.</summary>
        public int InformationRequested { get; set; }

        /// <summary>Projects where the customer answered and staff still have to review it.</summary>
        public int InformationSubmitted { get; set; }

        public IReadOnlyDictionary<ProjectStatus, int> ProjectCountsByStatus { get; set; }
            = new Dictionary<ProjectStatus, int>();

        public IReadOnlyList<DashboardRecentProjectDto> RecentProjects { get; set; }
            = Array.Empty<DashboardRecentProjectDto>();
    }

    /// <summary>Recent project row for dashboard lists (includes customer when scoped to partner/internal).</summary>
    public class DashboardRecentProjectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; }
        public ProjectStep CurrentStep { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>Set while this project is waiting to be deleted.</summary>
        public DateTimeOffset? DeletionScheduledFor { get; set; }

        /// <summary>The information request status that needs attention: Requested, else Submitted, else Approved.</summary>
        public InformationRequestStatus? InformationRequestStatus { get; set; }

        /// <summary>In account creation, with the users CSV asked for and not uploaded yet.</summary>
        public bool AwaitingUsersCsv { get; set; }

        /// <summary>The payment page is hosted by PCI Pal, so an account is created after provisioning.</summary>
        public bool RequiresAccount { get; set; }

        public Guid CustomerId { get; set; }
        public string? CustomerName { get; set; }
    }
}

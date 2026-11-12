using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>Filter and paging for a project list (one customer's, or all projects for internal staff).</summary>
    public class ProjectListQuery
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 50;

        public string? Search { get; set; }
        public ProjectStatus? Status { get; set; }

        /// <summary>The step the project has reached ("Progress").</summary>
        public ProjectStep? Step { get; set; }

        /// <summary>Only used by the all-projects list.</summary>
        public Guid? CustomerId { get; set; }

        /// <summary>All-projects list only: limits it to this partner's customers.</summary>
        public Guid? PartnerId { get; set; }

        /// <summary>Created on or after this day (UTC), inclusive.</summary>
        public DateOnly? CreatedFrom { get; set; }

        /// <summary>Created on or before this day (UTC), inclusive.</summary>
        public DateOnly? CreatedTo { get; set; }

        /// <summary>Prototype-style list tab (needs you, with PCI Pal, …).</summary>
        public ProjectListView? View { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
    }
}

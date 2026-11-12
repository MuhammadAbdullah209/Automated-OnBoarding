namespace AOB.BAL.DTOs.Customers
{
    /// <summary>Filter and paging for customer list screens (partner-scoped or admin-wide).</summary>
    public class CustomerListQuery
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 50;

        public string? Search { get; set; }

        /// <summary>When set, limits to customers with or without a login.</summary>
        public bool? HasLogin { get; set; }

        /// <summary>When set, limits to direct (self-signup) or partner-managed customers.</summary>
        public bool? IsDirect { get; set; }

        /// <summary>When set, limits to customers owned by this partner (admin list).</summary>
        public Guid? PartnerId { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
    }
}

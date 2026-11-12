namespace AOB.BAL.Common.Models
{
    /// <summary>A page of items plus the total that match the query (before paging).</summary>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int TotalCount { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

        public bool HasPreviousPage => Page > 1;

        public bool HasNextPage => Page < TotalPages;

        /// <summary>1-based index of the first item on this page, or 0 when empty.</summary>
        public int FirstItemIndex => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;

        /// <summary>1-based index of the last item on this page, or 0 when empty.</summary>
        public int LastItemIndex => TotalCount == 0 ? 0 : Math.Min(Page * PageSize, TotalCount);
    }
}

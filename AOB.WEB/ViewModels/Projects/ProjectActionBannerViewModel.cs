namespace AOB.WEB.ViewModels.Projects
{
    /// <summary>Customer action banner on project overview (upload users, respond to PCI Pal).</summary>
    public class ProjectActionBannerViewModel
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ButtonText { get; init; } = string.Empty;

        /// <summary>In-page anchor, e.g. #users-card.</summary>
        public string? AnchorHref { get; init; }

        public string? Action { get; init; }
        public IDictionary<string, string>? RouteValues { get; init; }
    }
}

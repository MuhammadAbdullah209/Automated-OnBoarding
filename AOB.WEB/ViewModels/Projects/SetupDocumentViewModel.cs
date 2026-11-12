using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Projects;

namespace AOB.WEB.ViewModels.Projects
{
    /// <summary>The Setup Documentation Pack page: the project's setup details and the customer they are for.</summary>
    public class SetupDocumentViewModel
    {
        public SetupDocumentDto Document { get; set; } = new();
        public CustomerProfileDto? Customer { get; set; }

        /// <summary>The date printed on the cover (the day the document is generated).</summary>
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Where the "Back to project" link goes.</summary>
        public string BackUrl { get; set; } = string.Empty;
    }
}

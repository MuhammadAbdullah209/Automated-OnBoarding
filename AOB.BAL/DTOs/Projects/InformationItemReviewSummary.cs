using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>Where a requested document stands, for the project documents list.</summary>
    public record InformationItemReviewSummary(
        InformationItemStatus Status,
        string? ReviewNote,
        Guid? ActiveDocumentId);
}

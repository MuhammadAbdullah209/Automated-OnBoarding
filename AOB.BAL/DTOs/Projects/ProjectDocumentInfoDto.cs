using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>Details of a file uploaded to a project (without its content).</summary>
    public class ProjectDocumentInfoDto
    {
        public Guid Id { get; set; }
        public ProjectDocumentType Type { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTimeOffset UploadedAt { get; set; }

        /// <summary>
        /// What the document is for, used to list documents by kind: the information that was requested (e.g. the
        /// merchant accounts, SMS or Email), or the document type's label for other uploads.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>When this file answers an information request item.</summary>
        public Guid? InformationRequestItemId { get; set; }

        /// <summary>
        /// This file's status: the item's status when it is the latest response's file, otherwise how the response
        /// it was sent with was reviewed (for example Rejected).
        /// </summary>
        public InformationItemStatus? ItemStatus { get; set; }

        /// <summary>The latest response's file, waiting for the internal user to approve, reject or resolve.</summary>
        public bool AwaitingReview { get; set; }

        /// <summary>Older upload for the same request item that was replaced before it was reviewed.</summary>
        public bool IsSuperseded { get; set; }
    }
}

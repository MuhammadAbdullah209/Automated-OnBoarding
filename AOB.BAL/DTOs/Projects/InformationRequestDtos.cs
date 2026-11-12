using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>One thing an internal user can ask the customer for (derived from why the build is custom).</summary>
    public class InformationRequestOptionDto
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        /// <summary>What the customer will be asked to provide, worded for the customer.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>The internal reason this is needed (for staff only); null for the generic options.</summary>
        public string? InternalReason { get; set; }
    }

    public class InformationRequestDto
    {
        public Guid Id { get; set; }
        public InformationRequestStatus Status { get; set; }
        public string? Instructions { get; set; }
        public string? RequestedByName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? SubmittedAt { get; set; }
        public string? CustomerNotes { get; set; }
        public IReadOnlyList<InformationRequestItemDto> Items { get; set; } = Array.Empty<InformationRequestItemDto>();
    }

    /// <summary>One requested document, with its latest response and its history.</summary>
    public class InformationRequestItemDto
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>The internal user's note for this document.</summary>
        public string? Instructions { get; set; }

        public string? RequestedByName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public InformationItemStatus Status { get; set; }

        /// <summary>The reviewer's note on the latest response (the rejection reason, or a note when resolved).</summary>
        public string? ReviewNote { get; set; }

        /// <summary>The customer's note with their latest response.</summary>
        public string? CustomerNote { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }

        /// <summary>The document attached to the latest response; null when the customer only sent a note.</summary>
        public Guid? DocumentId { get; set; }
        public string? DocumentName { get; set; }

        /// <summary>Earlier responses (before the latest one), newest first.</summary>
        public IReadOnlyList<InformationResponseDto> History { get; set; } = Array.Empty<InformationResponseDto>();

        /// <summary>The customer has responded and the internal user can approve, reject or resolve.</summary>
        public bool AwaitingReview => Status is InformationItemStatus.Submitted or InformationItemStatus.Resubmitted;

        /// <summary>Approve needs a document: a note-only response can only be rejected or resolved.</summary>
        public bool CanApprove => AwaitingReview && DocumentId is not null;

        /// <summary>True when the customer has to respond (first time, or again after a rejection).</summary>
        public bool NeedsCustomer => Status is InformationItemStatus.Pending or InformationItemStatus.Rejected;

        /// <summary>Approved or resolved: nothing more happens on this request.</summary>
        public bool IsClosed => Status is InformationItemStatus.Approved or InformationItemStatus.Resolved;
    }

    /// <summary>An earlier response to a requested document and how it was reviewed.</summary>
    public class InformationResponseDto
    {
        public DateTimeOffset SubmittedAt { get; set; }
        public string? Note { get; set; }
        public Guid? DocumentId { get; set; }
        public string? DocumentName { get; set; }
        public InformationItemStatus? Outcome { get; set; }
        public string? ReviewNote { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public string? ReviewedByName { get; set; }
    }

    /// <summary>One document the internal user asks for, with their optional note.</summary>
    public record InformationRequestDraft(string Code, string? Note);

    /// <summary>A file the customer chose for a requested document. The caller disposes the stream.</summary>
    public class InformationUploadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long Length { get; set; }
        public Stream Content { get; set; } = Stream.Null;
    }
}

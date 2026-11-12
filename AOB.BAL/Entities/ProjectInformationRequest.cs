using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// A document request from an internal user on a project waiting for approval (a custom build). Each request
    /// asks for one document (its item); older requests may hold several items. The customer responds to each item
    /// with a file, a note, or both, and the internal user approves, rejects or resolves each item on its own.
    /// Status follows the items, see ProjectInformationService.
    /// </summary>
    public class ProjectInformationRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        public InformationRequestStatus Status { get; set; } = InformationRequestStatus.Requested;

        /// <summary>Instructions shared by every item (older requests only; each item now has its own note).</summary>
        [MaxLength(2000)]
        public string? Instructions { get; set; }

        public Guid? RequestedByUserAuthId { get; set; }

        [MaxLength(200)]
        public string? RequestedByName { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>When the customer last submitted.</summary>
        public DateTimeOffset? SubmittedAt { get; set; }

        /// <summary>When the customer last used Provide information (files and/or notes). Cleared when staff ask again.</summary>
        public DateTimeOffset? CustomerRespondedAt { get; set; }

        /// <summary>The customer's note for the whole request (older requests only; notes are now kept per item).</summary>
        [MaxLength(2000)]
        public string? CustomerNotes { get; set; }
         
        public Project Project { get; set; } = null!;

        public ICollection<ProjectInformationRequestItem> Items { get; set; } = new List<ProjectInformationRequestItem>();
    }
}

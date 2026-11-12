using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// One requested document, e.g. "Payment gateway information". Each one has its own note, status and history:
    /// the customer responds to it on its own and the internal user approves, rejects or resolves it on its own.
    /// </summary>
    public class ProjectInformationRequestItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Request))]
        public Guid RequestId { get; set; }

        /// <summary>The build rule the item comes from (e.g. GATEWAY_STANDARD), or DOCUMENTS / OTHER.</summary>
        [Required, MaxLength(60)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>What to provide, worded for the customer.</summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>The internal user's optional note for this document.</summary>
        [MaxLength(2000)]
        public string? Instructions { get; set; }

        public InformationItemStatus Status { get; set; } = InformationItemStatus.Pending;

        /// <summary>The reviewer's note on the latest response: the reason for a rejection, or a note when resolving.</summary>
        [MaxLength(1000)]
        public string? ReviewNote { get; set; }

        /// <summary>The customer's note with their latest response.</summary>
        [MaxLength(2000)]
        public string? CustomerNote { get; set; }

        /// <summary>The document attached to the latest response; null when the customer only sent a note.</summary>
        public Guid? DocumentId { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public ProjectInformationRequest Request { get; set; } = null!;

        /// <summary>Every response the customer sent, with how it was reviewed.</summary>
        public ICollection<ProjectInformationResponse> Responses { get; set; } = new List<ProjectInformationResponse>();
    }
}

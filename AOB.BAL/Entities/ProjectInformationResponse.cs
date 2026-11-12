using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// One response from the customer to a requested document (a file, a note, or both) and how it was reviewed.
    /// A rejected document is answered again on the same request, so these rows keep the earlier attempts.
    /// </summary>
    public class ProjectInformationResponse
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Item))]
        public Guid ItemId { get; set; }

        [MaxLength(2000)]
        public string? Note { get; set; }

        /// <summary>The project document sent with this response, if any.</summary>
        public Guid? DocumentId { get; set; }

        public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Approved, Rejected or Resolved once reviewed; null while waiting for review.</summary>
        public InformationItemStatus? Outcome { get; set; }

        [MaxLength(1000)]
        public string? ReviewNote { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        [MaxLength(200)]
        public string? ReviewedByName { get; set; }

        public ProjectInformationRequestItem Item { get; set; } = null!;
    }
}

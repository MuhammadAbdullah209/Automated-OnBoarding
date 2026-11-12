using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// One recent-activity entry. Customer and project events share this table: every entry belongs to a
    /// customer, and project events also carry the project. Kept as a plain history; entries are never edited.
    /// </summary>
    public class ActivityLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CustomerId { get; set; }

        /// <summary>Set for project events; null for customer-level events.</summary>
        public Guid? ProjectId { get; set; }

        /// <summary>Whether the event is about the customer or one of their projects.</summary>
        public ActivityTarget Target { get; set; }

        /// <summary>Short title, e.g. "Project submitted".</summary>
        [Required, MaxLength(150)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Free-text reason / detail shown with the title.</summary>
        [MaxLength(1000)]
        public string? Reason { get; set; }

        /// <summary>Who did it (null when the system did, e.g. a derived status change).</summary>
        public Guid? ActorUserAuthId { get; set; }

        [MaxLength(200)]
        public string? ActorName { get; set; }

        [MaxLength(30)]
        public string? ActorRole { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

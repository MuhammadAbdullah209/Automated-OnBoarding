using System.ComponentModel.DataAnnotations;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// A kind of document or evidence customers can upload (e.g. Data Processing Agreement).
    /// Managed by internal users; customers see the active ones as their checklist.
    /// </summary>
    public class DocumentType
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // False = shown with an "Optional" badge.
        public bool IsRequired { get; set; }

        // Lower numbers are listed first.
        public int SortOrder { get; set; }

        // Inactive types are hidden from customers but keep their existing uploads.
        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<CustomerDocument> CustomerDocuments { get; set; } = new List<CustomerDocument>();
    }
}

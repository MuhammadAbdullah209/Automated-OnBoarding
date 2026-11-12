namespace AOB.BAL.DTOs.Documents
{
    /// <summary>One row of a customer's Documents / Evidence checklist.</summary>
    public class DocumentChecklistItemDto
    {
        public Guid DocumentTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsRequired { get; set; }

        /// <summary>True once the customer has uploaded a file for this type.</summary>
        public bool IsUploaded { get; set; }

        public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTimeOffset? UploadedAt { get; set; }
    }
}

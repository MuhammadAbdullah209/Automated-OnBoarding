using System.ComponentModel.DataAnnotations;

namespace AOB.BAL.DTOs.Documents
{
    /// <summary>Document type as edited on the CRUD screens (form model and service input).</summary>
    public class DocumentTypeDto
    {
        public Guid Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Required")]
        public bool IsRequired { get; set; }

        [Range(0, 10000), Display(Name = "Sort order")]
        public int SortOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        /// <summary>Number of customer uploads of this type (list screen only).</summary>
        public int UploadCount { get; set; }
    }
}

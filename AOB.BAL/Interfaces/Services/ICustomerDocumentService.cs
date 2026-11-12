using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Documents;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>A customer's Documents / Evidence checklist and uploads.</summary>
    public interface ICustomerDocumentService
    {
        /// <summary>Every active document type with the customer's upload status for it.</summary>
        Task<IReadOnlyList<DocumentChecklistItemDto>> GetChecklistAsync(Guid customerId);

        /// <summary>
        /// Stores the file for the customer and document type, replacing any earlier upload.
        /// Checks the extension and size limits from configuration.
        /// </summary>
        Task<ServiceResult> UploadAsync(Guid customerId, Guid documentTypeId, string fileName, string? contentType, long length, Stream content);

        /// <summary>Opens the customer's uploaded file for download.</summary>
        Task<ServiceResult<DocumentFileDto>> DownloadAsync(Guid customerId, Guid documentTypeId);

        /// <summary>Removes the customer's uploaded file for a document type.</summary>
        Task<ServiceResult> RemoveAsync(Guid customerId, Guid documentTypeId);
    }
}

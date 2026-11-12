using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Documents;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>CRUD for document types (managed by internal users).</summary>
    public interface IDocumentTypeService
    {
        Task<IReadOnlyList<DocumentTypeDto>> GetAllAsync();

        Task<ServiceResult<DocumentTypeDto>> GetAsync(Guid id);

        /// <summary>Creates a type. Fails if the name is already used.</summary>
        Task<ServiceResult> CreateAsync(DocumentTypeDto dto);

        /// <summary>Updates a type. Fails if the name is used by another type.</summary>
        Task<ServiceResult> UpdateAsync(DocumentTypeDto dto);

        /// <summary>Deletes a type. Fails if customers have uploaded documents of it (deactivate instead).</summary>
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}

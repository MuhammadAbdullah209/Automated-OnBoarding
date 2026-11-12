using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Document type data access.</summary>
    public interface IDocumentTypeRepository
    {
        /// <summary>All types ordered by SortOrder then name, with the number of uploads for each.</summary>
        Task<IReadOnlyList<(DocumentType Type, int UploadCount)>> GetAllWithUploadCountAsync();

        /// <summary>Active types in display order (the customer checklist).</summary>
        Task<IReadOnlyList<DocumentType>> GetActiveAsync();

        /// <summary>Finds a type by id (tracked, so changes can be saved).</summary>
        Task<DocumentType?> GetByIdAsync(Guid id);

        /// <summary>True if another type (not <paramref name="excludeId"/>) already uses the name.</summary>
        Task<bool> NameExistsAsync(string name, Guid? excludeId);

        /// <summary>True if any customer has uploaded a document of this type.</summary>
        Task<bool> HasUploadsAsync(Guid id);

        Task AddAsync(DocumentType documentType);

        void Remove(DocumentType documentType);
    }
}

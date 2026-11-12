using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Document type data access.</summary>
    public class DocumentTypeRepository(OnboardingDbContext context) : IDocumentTypeRepository
    {
        public async Task<IReadOnlyList<(DocumentType Type, int UploadCount)>> GetAllWithUploadCountAsync()
        {
            var rows = await context.DocumentTypes.AsNoTracking()
                .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
                .Select(t => new { Type = t, UploadCount = t.CustomerDocuments.Count })
                .ToListAsync();

            return rows.Select(r => (r.Type, r.UploadCount)).ToList();
        }

        public async Task<IReadOnlyList<DocumentType>> GetActiveAsync()
            => await context.DocumentTypes.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
                .ToListAsync();

        public async Task<DocumentType?> GetByIdAsync(Guid id)
            => await context.DocumentTypes.FindAsync(id);

        public async Task<bool> NameExistsAsync(string name, Guid? excludeId)
            => await context.DocumentTypes.AnyAsync(t => t.Name == name && t.Id != excludeId);

        public async Task<bool> HasUploadsAsync(Guid id)
            => await context.CustomerDocuments.AnyAsync(d => d.DocumentTypeId == id);

        public async Task AddAsync(DocumentType documentType)
            => await context.DocumentTypes.AddAsync(documentType);

        public void Remove(DocumentType documentType)
            => context.DocumentTypes.Remove(documentType);
    }
}

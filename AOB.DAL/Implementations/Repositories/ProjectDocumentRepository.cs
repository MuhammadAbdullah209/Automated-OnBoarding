using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Repositories
{
    /// <summary>Data access for files uploaded to projects.</summary>
    public class ProjectDocumentRepository(OnboardingDbContext context) : IProjectDocumentRepository
    {
        public async Task<ProjectDocument?> GetLatestByTypeAsync(Guid projectId, ProjectDocumentType type)
            => await context.ProjectDocuments
                .Where(d => d.ProjectId == projectId && d.Type == type)
                .OrderByDescending(d => d.UploadedAt)
                .FirstOrDefaultAsync();

        public async Task<IReadOnlyList<ProjectDocument>> ListByProjectAsync(Guid projectId)
            => await context.ProjectDocuments.AsNoTracking()
                .Where(d => d.ProjectId == projectId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

        public async Task<IReadOnlyDictionary<Guid, string>> GetInformationItemTitlesAsync(IReadOnlyCollection<Guid> itemIds)
            => itemIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await context.ProjectInformationRequestItems.AsNoTracking()
                    .Where(i => itemIds.Contains(i.Id))
                    .ToDictionaryAsync(i => i.Id, i => i.Title);

        public async Task<IReadOnlyDictionary<Guid, InformationItemReviewSummary>> GetInformationItemReviewSummariesAsync(IReadOnlyCollection<Guid> itemIds)
            => itemIds.Count == 0
                ? new Dictionary<Guid, InformationItemReviewSummary>()
                : await context.ProjectInformationRequestItems.AsNoTracking()
                    .Where(i => itemIds.Contains(i.Id))
                    .ToDictionaryAsync(i => i.Id, i => new InformationItemReviewSummary(i.Status, i.ReviewNote, i.DocumentId));

        public async Task<IReadOnlyDictionary<Guid, InformationItemStatus>> GetInformationResponseOutcomesAsync(IReadOnlyCollection<Guid> documentIds)
            => documentIds.Count == 0
                ? new Dictionary<Guid, InformationItemStatus>()
                : await context.ProjectInformationResponses.AsNoTracking()
                    .Where(r => r.DocumentId != null && documentIds.Contains(r.DocumentId.Value) && r.Outcome != null)
                    .ToDictionaryAsync(r => r.DocumentId!.Value, r => r.Outcome!.Value);

        public async Task AddAsync(ProjectDocument document)
            => await context.ProjectDocuments.AddAsync(document);
    }
}

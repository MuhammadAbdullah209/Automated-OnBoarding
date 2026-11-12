using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;

namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>Data access for files uploaded to projects.</summary>
    public interface IProjectDocumentRepository
    {
        /// <summary>The project's most recent document of a type (tracked, so it can be replaced), if any.</summary>
        Task<ProjectDocument?> GetLatestByTypeAsync(Guid projectId, ProjectDocumentType type);

        /// <summary>All documents for a project, newest first.</summary>
        Task<IReadOnlyList<ProjectDocument>> ListByProjectAsync(Guid projectId);

        /// <summary>The title of each information request item, by item id (what was asked for).</summary>
        Task<IReadOnlyDictionary<Guid, string>> GetInformationItemTitlesAsync(IReadOnlyCollection<Guid> itemIds);

        Task<IReadOnlyDictionary<Guid, InformationItemReviewSummary>> GetInformationItemReviewSummariesAsync(IReadOnlyCollection<Guid> itemIds);

        /// <summary>How each reviewed response was decided, by the document sent with it.</summary>
        Task<IReadOnlyDictionary<Guid, InformationItemStatus>> GetInformationResponseOutcomesAsync(IReadOnlyCollection<Guid> documentIds);

        /// <summary>Adds the document. Saved by IUnitOfWork.SaveChangesAsync.</summary>
        Task AddAsync(ProjectDocument document);
    }
}

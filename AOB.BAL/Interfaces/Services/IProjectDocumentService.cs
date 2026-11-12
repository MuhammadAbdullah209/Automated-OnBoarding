using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Documents;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>
    /// Files uploaded to a project (for now the API documentation for a web callback). Every method takes
    /// the customer id so a customer can only reach their own project's files.
    /// </summary>
    public interface IProjectDocumentService
    {
        /// <summary>The project's current document of this type, or null if there is none (or the project is not the customer's).</summary>
        Task<ProjectDocumentInfoDto?> GetAsync(Guid customerId, Guid projectId, ProjectDocumentType type);

        /// <summary>All documents uploaded to the project, if it belongs to the customer.</summary>
        Task<IReadOnlyList<ProjectDocumentInfoDto>> ListForProjectAsync(Guid customerId, Guid projectId);

        /// <summary>
        /// Stores the file for the project. An API documentation upload replaces the earlier one.
        /// Checks the extension and size limits from configuration; fails once the project is submitted.
        /// </summary>
        Task<ServiceResult> UploadAsync(Guid customerId, Guid projectId, ProjectDocumentType type,
            string fileName, string? contentType, long length, Stream content);

        /// <summary>Opens the project's document of this type for download.</summary>
        Task<ServiceResult<DocumentFileDto>> DownloadAsync(Guid customerId, Guid projectId, ProjectDocumentType type);
    }
}

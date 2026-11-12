using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Documents;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Files uploaded to a project, stored through IFileStorage with FileCategory.ProjectDocument.
    /// Limits come from the "FileStorage" section of appsettings.json: MaxFileSizeMB, and for API
    /// documentation ApiDocumentationExtensions (specs such as .json and .yaml are common there).
    /// </summary>
    public class ProjectDocumentService(
        IProjectRepository projectRepository,
        IProjectDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IActivityLogService activityLog,
        IFileStorage fileStorage,
        IConfiguration configuration,
        ILogger<ProjectDocumentService> logger) : IProjectDocumentService
    {
        private readonly long _maxFileSizeBytes = configuration.GetValue("FileStorage:MaxFileSizeMB", 10) * 1024L * 1024L;

        private readonly string[] _apiDocumentationExtensions =
            configuration.GetSection("FileStorage:ApiDocumentationExtensions").Get<string[]>()
            ?? new[] { ".pdf", ".json", ".yaml", ".yml", ".doc", ".docx", ".txt" };

        private readonly string[] _generalExtensions =
            configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
            ?? new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };

        public async Task<ProjectDocumentInfoDto?> GetAsync(Guid customerId, Guid projectId, ProjectDocumentType type)
        {
            if (await projectRepository.GetForCustomerAsync(customerId, projectId) is null)
            {
                return null;
            }

            var document = await documentRepository.GetLatestByTypeAsync(projectId, type);
            return document is null ? null : ToInfo(document);
        }

        public async Task<IReadOnlyList<ProjectDocumentInfoDto>> ListForProjectAsync(Guid customerId, Guid projectId)
        {
            if (await projectRepository.GetForCustomerAsync(customerId, projectId) is null)
            {
                return Array.Empty<ProjectDocumentInfoDto>();
            }

            var documents = await documentRepository.ListByProjectAsync(projectId);
            var itemIds = documents.Where(d => d.InformationRequestItemId is not null).Select(d => d.InformationRequestItemId!.Value).Distinct().ToList();
            var titles = await documentRepository.GetInformationItemTitlesAsync(itemIds);
            var reviews = await documentRepository.GetInformationItemReviewSummariesAsync(itemIds);
            var outcomes = await documentRepository.GetInformationResponseOutcomesAsync(
                documents.Where(d => d.InformationRequestItemId is not null).Select(d => d.Id).ToList());

            return documents.Select(d =>
            {
                var info = ToInfo(d);
                if (d.InformationRequestItemId is Guid itemId)
                {
                    info.InformationRequestItemId = itemId;
                    info.Category = titles.TryGetValue(itemId, out var title)
                        ? title
                        : ProjectOptions.ProjectDocumentTypeLabels[d.Type];
                    if (reviews.TryGetValue(itemId, out var review) && review.ActiveDocumentId == d.Id)
                    {
                        // The file of the customer's latest response: it carries the request's status.
                        info.ItemStatus = review.Status;
                        info.AwaitingReview = review.Status is InformationItemStatus.Submitted or InformationItemStatus.Resubmitted;
                    }
                    else if (outcomes.TryGetValue(d.Id, out var outcome))
                    {
                        // An earlier response: keep how it was reviewed (usually Rejected).
                        info.ItemStatus = outcome;
                    }
                    else
                    {
                        info.IsSuperseded = true;
                    }
                }
                else
                {
                    info.Category = ProjectOptions.ProjectDocumentTypeLabels[d.Type];
                }

                return info;
            }).ToList();
        }

        private static ProjectDocumentInfoDto ToInfo(ProjectDocument document) => new()
        {
            Id = document.Id,
            Type = document.Type,
            FileName = document.OriginalFileName,
            FileSizeBytes = document.FileSizeBytes,
            UploadedAt = document.UploadedAt
        };

        public async Task<ServiceResult> UploadAsync(Guid customerId, Guid projectId, ProjectDocumentType type,
            string fileName, string? contentType, long length, Stream content)
        {
            var project = await projectRepository.GetForCustomerAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }
            if (!ProjectStatusRules.CanCustomerProvideMissingItems(project.Status))
            {
                return ServiceResult.Failure("This project is already being provisioned and can no longer be changed.");
            }

            if (length <= 0)
            {
                return ServiceResult.Failure("The file is empty.");
            }
            if (length > _maxFileSizeBytes)
            {
                return ServiceResult.Failure($"The file is too large. The limit is {_maxFileSizeBytes / 1024 / 1024} MB.");
            }

            // Only the extension and display name are taken from the upload; the stored name is generated.
            var safeName = Path.GetFileName(fileName);
            var extension = Path.GetExtension(safeName).ToLowerInvariant();
            var allowed = type == ProjectDocumentType.ApiDocumentation ? _apiDocumentationExtensions : _generalExtensions;
            if (!allowed.Contains(extension))
            {
                return ServiceResult.Failure($"This file type is not allowed. Allowed: {string.Join(", ", allowed)}.");
            }

            if (!await AOB.DAL.Common.FileSignatureValidator.MatchesExtensionAsync(extension, content))
            {
                return ServiceResult.Failure($"The file does not look like a valid {extension} file.");
            }

            // The type sent by the browser is not trusted; it is derived from the validated extension.
            contentType = AOB.DAL.Common.FileSignatureValidator.ContentTypeFor(extension);

            var storedPath = await fileStorage.SaveAsync(FileCategory.ProjectDocument, projectId, extension, content);
            string? replacedPath = null;

            try
            {
                // API documentation is one file per project: a new upload replaces the earlier one.
                var existing = type == ProjectDocumentType.ApiDocumentation
                    ? await documentRepository.GetLatestByTypeAsync(projectId, type)
                    : null;

                if (existing is null)
                {
                    await documentRepository.AddAsync(new ProjectDocument
                    {
                        ProjectId = projectId,
                        Type = type,
                        OriginalFileName = safeName,
                        StoredFilePath = storedPath,
                        ContentType = contentType,
                        FileSizeBytes = length
                    });
                }
                else
                {
                    replacedPath = existing.StoredFilePath;
                    existing.OriginalFileName = safeName;
                    existing.StoredFilePath = storedPath;
                    existing.ContentType = contentType;
                    existing.FileSizeBytes = length;
                    existing.UploadedAt = DateTimeOffset.UtcNow;
                }

                await unitOfWork.SaveChangesAsync();
                await activityLog.LogProjectAsync(customerId, projectId, existing is null ? "Document uploaded" : "Document replaced", $"API documentation: {safeName}");
            }
            catch
            {
                // Database save failed, so do not leave the new file behind.
                await fileStorage.DeleteAsync(storedPath);
                throw;
            }

            if (replacedPath is not null)
            {
                try
                {
                    await fileStorage.DeleteAsync(replacedPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not delete replaced file {Path}.", replacedPath);
                }
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<DocumentFileDto>> DownloadAsync(Guid customerId, Guid projectId, ProjectDocumentType type)
        {
            if (await projectRepository.GetForCustomerAsync(customerId, projectId) is null)
            {
                return ServiceResult<DocumentFileDto>.Failure("Document not found.");
            }

            var document = await documentRepository.GetLatestByTypeAsync(projectId, type);
            if (document is null)
            {
                return ServiceResult<DocumentFileDto>.Failure("Document not found.");
            }

            var stream = await fileStorage.OpenReadAsync(document.StoredFilePath);
            if (stream is null)
            {
                return ServiceResult<DocumentFileDto>.Failure("The file is no longer available.");
            }

            return ServiceResult<DocumentFileDto>.Success(new DocumentFileDto
            {
                Content = stream,
                FileName = document.OriginalFileName,
                ContentType = document.ContentType ?? "application/octet-stream"
            });
        }
    }
}

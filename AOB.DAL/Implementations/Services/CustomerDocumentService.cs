using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Documents;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Customer document checklist and uploads. Limits come from the "FileStorage" section of
    /// appsettings.json (MaxFileSizeMB, AllowedExtensions).
    /// </summary>
    public class CustomerDocumentService(
        IDocumentTypeRepository documentTypeRepository,
        ICustomerDocumentRepository customerDocumentRepository,
        ICustomerStatusService customerStatusService,
        IActivityLogService activityLog,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IConfiguration configuration,
        ILogger<CustomerDocumentService> logger) : ICustomerDocumentService
    {
        private readonly long _maxFileSizeBytes = configuration.GetValue("FileStorage:MaxFileSizeMB", 10) * 1024L * 1024L;

        private readonly string[] _allowedExtensions =
            configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
            ?? new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };

        public async Task<IReadOnlyList<DocumentChecklistItemDto>> GetChecklistAsync(Guid customerId)
        {
            var types = await documentTypeRepository.GetActiveAsync();
            // The checklist is per document type, so documents without a type are not part of it.
            var uploads = (await customerDocumentRepository.GetByCustomerAsync(customerId))
                .Where(d => d.DocumentTypeId.HasValue)
                .ToDictionary(d => d.DocumentTypeId!.Value);

            return types.Select(t =>
            {
                uploads.TryGetValue(t.Id, out var upload);
                return new DocumentChecklistItemDto
                {
                    DocumentTypeId = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    IsRequired = t.IsRequired,
                    IsUploaded = upload is not null,
                    FileName = upload?.OriginalFileName,
                    FileSizeBytes = upload?.FileSizeBytes,
                    UploadedAt = upload?.UploadedAt
                };
            }).ToList();
        }

        public async Task<ServiceResult> UploadAsync(
            Guid customerId, Guid documentTypeId, string fileName, string? contentType, long length, Stream content)
        {
            var type = await documentTypeRepository.GetByIdAsync(documentTypeId);
            if (type is null || !type.IsActive)
            {
                return ServiceResult.Failure("Unknown document type.");
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
            if (!_allowedExtensions.Contains(extension))
            {
                return ServiceResult.Failure(AOB.BAL.Common.Constants.CustomerDocumentMessages.UnsupportedFileTypeBody);
            }

            if (!await AOB.DAL.Common.FileSignatureValidator.MatchesExtensionAsync(extension, content))
            {
                return ServiceResult.Failure($"The file does not look like a valid {extension} file.");
            }

            // The type sent by the browser is not trusted; it is derived from the validated extension.
            contentType = AOB.DAL.Common.FileSignatureValidator.ContentTypeFor(extension);

            var storedPath = await fileStorage.SaveAsync(FileCategory.CustomerDocument, customerId, extension, content);
            string? replacedPath = null;

            try
            {
                var existing = await customerDocumentRepository.GetAsync(customerId, documentTypeId);
                if (existing is null)
                {
                    await customerDocumentRepository.AddAsync(new CustomerDocument
                    {
                        CustomerId = customerId,
                        DocumentTypeId = documentTypeId,
                        OriginalFileName = safeName,
                        StoredFilePath = storedPath,
                        ContentType = contentType,
                        FileSizeBytes = length
                    });
                }
                else
                {
                    // Uploading again replaces the earlier file.
                    replacedPath = existing.StoredFilePath;
                    existing.OriginalFileName = safeName;
                    existing.StoredFilePath = storedPath;
                    existing.ContentType = contentType;
                    existing.FileSizeBytes = length;
                    existing.UploadedAt = DateTimeOffset.UtcNow;
                }

                await unitOfWork.SaveChangesAsync();
                await activityLog.LogCustomerAsync(customerId, existing is null ? "Document uploaded" : "Document replaced", $"{type.Name}: {safeName}");
                await customerStatusService.RefreshDerivedStatusAsync(customerId);
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

        public async Task<ServiceResult<DocumentFileDto>> DownloadAsync(Guid customerId, Guid documentTypeId)
        {
            var document = await customerDocumentRepository.GetAsync(customerId, documentTypeId);
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

        public async Task<ServiceResult> RemoveAsync(Guid customerId, Guid documentTypeId)
        {
            var type = await documentTypeRepository.GetByIdAsync(documentTypeId);
            if (type is null || !type.IsActive)
            {
                return ServiceResult.Failure("Unknown document type.");
            }

            var document = await customerDocumentRepository.GetAsync(customerId, documentTypeId);
            if (document is null)
            {
                return ServiceResult.Failure("Document not found.");
            }

            var storedPath = document.StoredFilePath;
            customerDocumentRepository.Remove(document);
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogCustomerAsync(customerId, "Document removed", $"{type.Name}: {document.OriginalFileName}");
            await customerStatusService.RefreshDerivedStatusAsync(customerId);

            try
            {
                await fileStorage.DeleteAsync(storedPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete file {Path} after document removal.", storedPath);
            }

            return ServiceResult.Success();
        }
    }
}

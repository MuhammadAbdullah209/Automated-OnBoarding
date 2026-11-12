using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Documents;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>CRUD for document types.</summary>
    public class DocumentTypeService(IDocumentTypeRepository documentTypeRepository, IUnitOfWork unitOfWork) : IDocumentTypeService
    {
        public async Task<IReadOnlyList<DocumentTypeDto>> GetAllAsync()
        {
            var rows = await documentTypeRepository.GetAllWithUploadCountAsync();
            return rows.Select(r => ToDto(r.Type, r.UploadCount)).ToList();
        }

        public async Task<ServiceResult<DocumentTypeDto>> GetAsync(Guid id)
        {
            var type = await documentTypeRepository.GetByIdAsync(id);
            return type is null
                ? ServiceResult<DocumentTypeDto>.Failure("Document type not found.")
                : ServiceResult<DocumentTypeDto>.Success(ToDto(type, 0));
        }

        public async Task<ServiceResult> CreateAsync(DocumentTypeDto dto)
        {
            var name = dto.Name.Trim();
            if (await documentTypeRepository.NameExistsAsync(name, null))
            {
                return ServiceResult.Failure("A document type with this name already exists.");
            }

            await documentTypeRepository.AddAsync(new DocumentType
            {
                Name = name,
                Description = Clean(dto.Description),
                IsRequired = dto.IsRequired,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            });
            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateAsync(DocumentTypeDto dto)
        {
            var type = await documentTypeRepository.GetByIdAsync(dto.Id);
            if (type is null)
            {
                return ServiceResult.Failure("Document type not found.");
            }

            var name = dto.Name.Trim();
            if (await documentTypeRepository.NameExistsAsync(name, dto.Id))
            {
                return ServiceResult.Failure("A document type with this name already exists.");
            }

            type.Name = name;
            type.Description = Clean(dto.Description);
            type.IsRequired = dto.IsRequired;
            type.SortOrder = dto.SortOrder;
            type.IsActive = dto.IsActive;
            type.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            var type = await documentTypeRepository.GetByIdAsync(id);
            if (type is null)
            {
                return ServiceResult.Failure("Document type not found.");
            }

            if (await documentTypeRepository.HasUploadsAsync(id))
            {
                return ServiceResult.Failure("Customers have uploaded documents of this type, so it cannot be deleted. Mark it inactive instead.");
            }

            documentTypeRepository.Remove(type);
            await unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        private static DocumentTypeDto ToDto(DocumentType type, int uploadCount) => new()
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            IsRequired = type.IsRequired,
            SortOrder = type.SortOrder,
            IsActive = type.IsActive,
            UploadCount = uploadCount
        };

        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>Customer management on behalf of partners.</summary>
    public class CustomerService : ICustomerService
    {
        private readonly IPartnerRepository _partnerRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ICustomerDocumentService _customerDocumentService;
        private readonly IProjectRepository _projectRepository;
        private readonly ICustomerStatusService _customerStatusService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _activityLog;

        public CustomerService(
            IPartnerRepository partnerRepository,
            ICustomerRepository customerRepository,
            ICustomerDocumentService customerDocumentService,
            IProjectRepository projectRepository,
            ICustomerStatusService customerStatusService,
            IUnitOfWork unitOfWork,
            IActivityLogService activityLog)
        {
            _partnerRepository = partnerRepository;
            _customerRepository = customerRepository;
            _customerDocumentService = customerDocumentService;
            _projectRepository = projectRepository;
            _customerStatusService = customerStatusService;
            _unitOfWork = unitOfWork;
            _activityLog = activityLog;
        }

        public async Task<ServiceResult<Guid>> CreatePartnerCustomerAsync(Guid partnerId, string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                return ServiceResult<Guid>.Failure("Company name is required.");
            }

            var partner = await _partnerRepository.GetByIdAsync(partnerId);
            if (partner is null || !partner.IsActive)
            {
                return ServiceResult<Guid>.Failure("Partner not found or inactive.");
            }

            // Non-direct customer: only the company name is captured, everything else stays null.
            var customer = new Customer
            {
                CompanyName = companyName.Trim(),
                IsDirect = false,
                PartnerId = partnerId
            };

            await _customerRepository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            await _activityLog.LogCustomerAsync(customer.Id, "Customer created", $"{customer.CompanyName} was added by partner {partner.Name}.");
            return ServiceResult<Guid>.Success(customer.Id);
        }

        public async Task<PagedResult<CustomerSummaryDto>> GetPartnerCustomersPagedAsync(Guid partnerId, CustomerListQuery query)
        {
            var normalized = NormalizeListQuery(query);
            var (customers, totalCount) = await _customerRepository.ListByPartnerAsync(partnerId, normalized);

            var totalPages = normalized.PageSize > 0
                ? (int)Math.Ceiling(totalCount / (double)normalized.PageSize)
                : 0;
            var page = normalized.Page;
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
                normalized.Page = page;
                (customers, totalCount) = await _customerRepository.ListByPartnerAsync(partnerId, normalized);
            }

            var summaries = customers.Select(ToSummary).ToList();
            await ApplyProjectStatsAsync(summaries);

            return new PagedResult<CustomerSummaryDto>
            {
                Items = summaries,
                TotalCount = totalCount,
                Page = page,
                PageSize = normalized.PageSize
            };
        }

        public async Task<PagedResult<CustomerSummaryDto>> GetAllCustomersPagedAsync(CustomerListQuery query)
        {
            var normalized = NormalizeListQuery(query);
            var (customers, totalCount) = await _customerRepository.ListAllAsync(normalized);

            var totalPages = normalized.PageSize > 0
                ? (int)Math.Ceiling(totalCount / (double)normalized.PageSize)
                : 0;
            var page = normalized.Page;
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
                normalized.Page = page;
                (customers, totalCount) = await _customerRepository.ListAllAsync(normalized);
            }

            var summaries = customers.Select(ToSummary).ToList();
            await ApplyProjectStatsAsync(summaries);

            return new PagedResult<CustomerSummaryDto>
            {
                Items = summaries,
                TotalCount = totalCount,
                Page = page,
                PageSize = normalized.PageSize
            };
        }

        public async Task<ServiceResult<CustomerPartnerDetailDto>> GetPartnerCustomerDetailsAsync(Guid partnerId, Guid customerId)
        {
            var customer = await _customerRepository.GetByPartnerAndIdAsync(partnerId, customerId);
            if (customer is null)
            {
                return ServiceResult<CustomerPartnerDetailDto>.Failure("Customer not found.");
            }

            await _customerStatusService.RefreshDerivedStatusAsync(customerId);
            customer = await _customerRepository.GetByPartnerAndIdAsync(partnerId, customerId);
            if (customer is null)
            {
                return ServiceResult<CustomerPartnerDetailDto>.Failure("Customer not found.");
            }

            var profileResult = await GetProfileAsync(customerId);
            if (!profileResult.Succeeded || profileResult.Data is null)
            {
                return ServiceResult<CustomerPartnerDetailDto>.Failure(profileResult.Error ?? "Customer not found.");
            }

            var projects = await _projectRepository.GetByCustomerAsync(customerId);
            var documents = await _customerDocumentService.GetChecklistAsync(customerId);

            return ServiceResult<CustomerPartnerDetailDto>.Success(new CustomerPartnerDetailDto
            {
                Id = customer.Id,
                Status = customer.Status,
                PartnerId = customer.PartnerId,
                CreatedAt = customer.CreatedAt,
                IsDirect = customer.IsDirect,
                HasLogin = customer.UserAuthId.HasValue,
                LoginEmail = customer.Auth?.Email,
                Profile = profileResult.Data,
                Documents = documents,
                Projects = projects.Select(ToProjectSummary).ToList()
            });
        }

        public async Task<IReadOnlyList<CustomerOptionDto>> GetCustomerOptionsAsync(Guid? partnerId = null)
            => await _customerRepository.GetOptionsAsync(partnerId);

        public async Task<Guid?> GetOwningPartnerIdAsync(Guid customerId)
            => (await _customerRepository.GetByIdAsync(customerId))?.PartnerId;

        public async Task<bool> PartnerOwnsCustomerAsync(Guid partnerId, Guid customerId)
            => await _customerRepository.GetByPartnerAndIdAsync(partnerId, customerId) is not null;

        public async Task<IReadOnlyList<DirectCustomerSummaryDto>> GetDirectCustomersAsync()
            => await _customerRepository.GetDirectSummariesAsync();

        public async Task<ServiceResult<CustomerProfileDto>> GetProfileAsync(Guid customerId)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer is null)
            {
                return ServiceResult<CustomerProfileDto>.Failure("Customer not found.");
            }

            return ServiceResult<CustomerProfileDto>.Success(new CustomerProfileDto
            {
                CompanyName = customer.CompanyName,
                CompanyRegistrationNumber = customer.CompanyRegistrationNumber,
                RegisteredCountryCode = customer.RegisteredCountryCode,
                IndustrySector = customer.IndustrySector,
                CompanyWebsite = customer.CompanyWebsite,
                PrimaryContactFullName = customer.PrimaryContactFullName,
                PrimaryContactJobTitle = customer.PrimaryContactJobTitle,
                PrimaryContactEmail = customer.PrimaryContactEmail,
                PrimaryContactPhoneNumber = customer.PrimaryContactPhoneNumber,
                TechnicalContactFullName = customer.TechnicalContactFullName,
                TechnicalContactJobTitle = customer.TechnicalContactJobTitle,
                TechnicalContactEmail = customer.TechnicalContactEmail,
                TechnicalContactPhoneNumber = customer.TechnicalContactPhoneNumber,
                OperatingPattern = customer.OperatingPattern,
                PrimaryTimeZone = customer.PrimaryTimeZone,
                HasTechnologyPartner = customer.HasTechnologyPartner
            });
        }

        public async Task<ServiceResult> SaveProfileAsync(Guid customerId, CustomerProfileDto profile)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer is null)
            {
                return ServiceResult.Failure("Customer not found.");
            }

            customer.CompanyName = Clean(profile.CompanyName);
            customer.CompanyRegistrationNumber = Clean(profile.CompanyRegistrationNumber);
            customer.RegisteredCountryCode = Clean(profile.RegisteredCountryCode)?.ToUpperInvariant();
            customer.IndustrySector = Clean(profile.IndustrySector);
            customer.CompanyWebsite = Clean(profile.CompanyWebsite);
            customer.PrimaryContactFullName = Clean(profile.PrimaryContactFullName);
            customer.PrimaryContactJobTitle = Clean(profile.PrimaryContactJobTitle);
            customer.PrimaryContactEmail = Clean(profile.PrimaryContactEmail);
            customer.PrimaryContactPhoneNumber = Clean(profile.PrimaryContactPhoneNumber);
            customer.TechnicalContactFullName = Clean(profile.TechnicalContactFullName);
            customer.TechnicalContactJobTitle = Clean(profile.TechnicalContactJobTitle);
            customer.TechnicalContactEmail = Clean(profile.TechnicalContactEmail);
            customer.TechnicalContactPhoneNumber = Clean(profile.TechnicalContactPhoneNumber);
            customer.OperatingPattern = Clean(profile.OperatingPattern);
            customer.PrimaryTimeZone = Clean(profile.PrimaryTimeZone);
            customer.HasTechnologyPartner = profile.HasTechnologyPartner;
            customer.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _activityLog.LogCustomerAsync(customerId, "Profile updated", "Organisation and contact details were saved.");
            await _customerStatusService.RefreshDerivedStatusAsync(customerId);
            return ServiceResult.Success();
        }

        public Task<ServiceResult> SetCustomerDisabledAsync(Guid customerId, bool disabled)
            => _customerStatusService.SetDisabledAsync(customerId, disabled);

        /// <summary>Trims text and turns blank input into null so empty fields are stored as NULL.</summary>
        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static CustomerListQuery NormalizeListQuery(CustomerListQuery query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? CustomerListQuery.DefaultPageSize : query.PageSize;
            if (pageSize > CustomerListQuery.MaxPageSize)
            {
                pageSize = CustomerListQuery.MaxPageSize;
            }

            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

            return new CustomerListQuery
            {
                Search = search,
                HasLogin = query.HasLogin,
                IsDirect = query.IsDirect,
                PartnerId = query.PartnerId,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task ApplyProjectStatsAsync(IList<CustomerSummaryDto> summaries)
        {
            if (summaries.Count == 0)
            {
                return;
            }

            var byCustomer = await _projectRepository.GetProjectStatusesByCustomerIdsAsync(
                summaries.Select(s => s.Id).ToList());

            foreach (var summary in summaries)
            {
                if (!byCustomer.TryGetValue(summary.Id, out var statuses))
                {
                    summary.ProjectCount = 0;
                    continue;
                }

                summary.ProjectCount = statuses.Count;
            }
        }

        private static CustomerSummaryDto ToSummary(Customer c) => new()
        {
            Id = c.Id,
            CompanyName = c.CompanyName,
            PrimaryContactFullName = c.PrimaryContactFullName,
            IsDirect = c.IsDirect,
            HasLogin = c.UserAuthId.HasValue,
            CreatedAt = c.CreatedAt,
            ProjectCount = 0,
            Status = c.Status,
            PartnerId = c.PartnerId,
            PartnerName = c.Partner?.Name
        };

        private static ProjectSummaryDto ToProjectSummary(Project p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Services = EnumCsv.Parse<PciPalService>(p.Services),
            Status = p.Status,
            CurrentStep = p.CurrentStep,
            CreatedAt = p.CreatedAt,
            DeletionScheduledFor = p.DeletionScheduledFor
        };
    }
}

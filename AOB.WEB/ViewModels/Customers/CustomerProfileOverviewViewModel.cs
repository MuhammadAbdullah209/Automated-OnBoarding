using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Documents;

namespace AOB.WEB.ViewModels.Customers
{
    public class CustomerProfileOverviewViewModel
    {
        public CustomerProfileDto Profile { get; set; } = new();

        public CustomerStatus Status { get; set; }

        public IReadOnlyList<DocumentChecklistItemDto> Documents { get; set; } = Array.Empty<DocumentChecklistItemDto>();

        /// <summary>Set when a partner or internal user views a customer's profile (read-only).</summary>
        public Guid? ViewedCustomerId { get; set; }

        public bool IsOwnProfile => !ViewedCustomerId.HasValue;

        public bool OrganisationComplete => CustomerProfileCompletionRules.HasRequiredCompanyFields(Profile);

        public bool ContactsAdded => CustomerProfileCompletionRules.HasAnyContactDetails(Profile);

        public int RequiredDocumentsMissing => CustomerProfileCompletionRules.RequiredDocumentsMissingCount(Documents);

        public bool DocumentsComplete => CustomerProfileCompletionRules.AreRequiredDocumentsComplete(Documents);

        public int DocumentsUploadedCount => Documents.Count(d => d.IsUploaded);

        /// <summary>When set (company, pc, tc, hours, partner), that section renders inline edit like the prototype.</summary>
        public string? EditSection { get; set; }

        public static CustomerProfileOverviewViewModel FromPartnerDetail(CustomerPartnerDetailDto detail)
            => new()
            {
                Profile = detail.Profile,
                Status = detail.Status,
                Documents = detail.Documents,
                ViewedCustomerId = detail.Id
            };
    }
}

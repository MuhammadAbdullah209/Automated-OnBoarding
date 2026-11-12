using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>"Business &amp; Use-Case Discovery" screen (form model and service input).</summary>
    public class ProjectDiscoveryDto : IValidatableObject
    {
        [MinLength(1, ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "PCI Pal services")]
        public List<PciPalService> Services { get; set; } = new();

        [Required(ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "What system does your team use")]
        public CrmSystem? CrmSystem { get; set; }

        [MinLength(1, ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "How customers pay")]
        public List<CurrentPaymentMethod> CurrentPaymentMethods { get; set; } = new();

        [StringLength(2000)]
        [Display(Name = "Describe your payment journey")]
        public string? PaymentJourneyDescription { get; set; }

        [Display(Name = "Payment experience in your CRM")]
        public CrmPaymentExperience? CrmPaymentExperience { get; set; }

        /// <summary>Portal URLs, one per line. Needed (and only kept) when the payment form is embedded in the CRM.</summary>
        [StringLength(4000)]
        [Display(Name = "Portal URLs")]
        public string? PortalUrls { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Asked for every project: it decides whether an account is created after provisioning.
            if (CrmPaymentExperience is null)
            {
                yield return new ValidationResult(
                    "Choose how the payment page should be shown.",
                    [nameof(CrmPaymentExperience)]);
            }

            if (CrmPaymentExperience == Common.Enums.CrmPaymentExperience.EmbeddedIframe
                && string.IsNullOrWhiteSpace(PortalUrls))
            {
                yield return new ValidationResult(ValidationMessages.Required, [nameof(PortalUrls)]);
            }

            if (CurrentPaymentMethods.Contains(CurrentPaymentMethod.AnotherWay)
                && string.IsNullOrWhiteSpace(PaymentJourneyDescription))
            {
                yield return new ValidationResult(
                    "Describe your payment journey, because you selected “Another way”.",
                    [nameof(PaymentJourneyDescription)]);
            }
        }
    }
}

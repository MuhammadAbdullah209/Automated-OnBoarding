using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Validation;

namespace AOB.BAL.DTOs.Customers
{
    /// <summary>
    /// The customer's "Organisation &amp; Contacts" details. Used both as the form model and as
    /// the data passed to the service, so validation rules live in one place.
    /// </summary>
    public class CustomerProfileDto
    {
        // ----- Company / organisation details -----

        [Required, StringLength(200), Display(Name = "Company name")]
        public string? CompanyName { get; set; }

        [Required, StringLength(120), Display(Name = "Company registration number")]
        public string? CompanyRegistrationNumber { get; set; }

        // ISO 3166 two-letter code, e.g. GB.
        [Required(ErrorMessage = "Select the registered country."), StringLength(2), Display(Name = "Registered country")]
        public string? RegisteredCountryCode { get; set; }

        [StringLength(120), Display(Name = "Industry sector")]
        public string? IndustrySector { get; set; }

        [StringLength(500), Display(Name = "Company website")]
        public string? CompanyWebsite { get; set; }

        // ----- Primary project contact -----

        [Required, StringLength(200), Display(Name = "Full name")]
        public string? PrimaryContactFullName { get; set; }

        [StringLength(120), Display(Name = "Job title / role")]
        public string? PrimaryContactJobTitle { get; set; }

        [Required(ErrorMessage = "Enter an email address.")]
        [RegularExpression(StrictEmailValidation.Pattern, ErrorMessage = StrictEmailValidation.ErrorMessage)]
        [StringLength(320)]
        [Display(Name = "Email address")]
        public string? PrimaryContactEmail { get; set; }

        [Phone, StringLength(40), Display(Name = "Phone number")]
        public string? PrimaryContactPhoneNumber { get; set; }

        // ----- Technical / support contact -----

        [Required, StringLength(200), Display(Name = "Full name")]
        public string? TechnicalContactFullName { get; set; }

        [StringLength(120), Display(Name = "Job title / role")]
        public string? TechnicalContactJobTitle { get; set; }

        [Required(ErrorMessage = "Enter an email address.")]
        [RegularExpression(StrictEmailValidation.Pattern, ErrorMessage = StrictEmailValidation.ErrorMessage)]
        [StringLength(320)]
        [Display(Name = "Email address")]
        public string? TechnicalContactEmail { get; set; }

        [Phone, StringLength(40), Display(Name = "Phone number")]
        public string? TechnicalContactPhoneNumber { get; set; }

        // ----- Operating / payment hours -----

        // Stable code from CustomerFormOptions.OperatingPatterns.
        [StringLength(60), Display(Name = "Operating pattern")]
        public string? OperatingPattern { get; set; }

        // IANA id, e.g. Europe/London.
        [StringLength(100), Display(Name = "Primary timezone")]
        public string? PrimaryTimeZone { get; set; }

        // ----- Technology partner -----

        [Display(Name = "We are working with a technology partner on this deployment")]
        public bool HasTechnologyPartner { get; set; }
    }
}

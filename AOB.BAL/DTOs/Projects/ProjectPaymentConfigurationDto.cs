using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>
    /// "Payment Configuration" screen (form model and service input). Never enter card numbers,
    /// CVV codes, production API keys or other live credentials here.
    /// </summary>
    public class ProjectPaymentConfigurationDto
    {
        [Required(ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "PSP / Gateway")]
        public Guid? PaymentGatewayId { get; set; }

        [Display(Name = "Number of attempts")]
        public int? MaxPaymentAttempts { get; set; }

        [MinLength(1, ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "Supported cards")]
        public List<CardScheme> SupportedCards { get; set; } = new();

        [Range(1, 1000, ErrorMessage = "Enter a whole number from 1 to 1000.")]
        [Display(Name = "Total merchant accounts")]
        public int? TotalMerchantAccounts { get; set; }

        [Display(Name = "How PCI Pal will identify between the merchants")]
        public MerchantIdentification? MerchantIdentification { get; set; }

        [StringLength(1000)]
        [Display(Name = "Describe how the merchants are identified")]
        public string? MerchantIdentificationOther { get; set; }

        [StringLength(200)]
        [Display(Name = "Customer acquirer")]
        public string? CustomerAcquirer { get; set; }

        public List<PaymentType> PaymentTypes { get; set; } = new();

        public List<CurrencyCode> Currencies { get; set; } = new();

        [Display(Name = "Card number masking")]
        public CardNumberMasking? CardNumberMasking { get; set; }

        [Display(Name = "SMS service provider")]
        public SmsServiceProvider? SmsServiceProvider { get; set; }

        [Display(Name = "SMS provider")]
        public OtherSmsProvider? OtherSmsProvider { get; set; }

        [Display(Name = "Email service provider")]
        public EmailServiceProvider? EmailServiceProvider { get; set; }

        [Display(Name = "Email provider")]
        public OtherEmailProvider? OtherEmailProvider { get; set; }

        [Display(Name = "A test or sandbox environment is available for integration testing")]
        public bool HasSandboxEnvironment { get; set; }
    }
}

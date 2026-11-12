using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// Answers from the "Payment Configuration" screen (1:1 with Project). Never store card numbers,
    /// CVV codes, production API keys or other live credentials here.
    /// </summary>
    public class ProjectPaymentConfiguration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        // "PSP / Gateway": one of the configured PaymentGateways. Also decides which gateway fields
        // apply (see ProjectGatewayFields).
        public Guid? PaymentGatewayId { get; set; }

        public PaymentGateway? PaymentGateway { get; set; }

        // "Supported Cards." Comma-separated CardScheme names.
        [MaxLength(200)]
        public string? SupportedCards { get; set; }

        // Maximum payment attempts per session (1–5), or 0 for unlimited.
        public int? MaxPaymentAttempts { get; set; }

        // "Total merchant accounts" across all PSPs.
        public int? TotalMerchantAccounts { get; set; }

        // With more than one merchant account: how PCI Pal tells them apart (currency, country or something else).
        public MerchantIdentification? MerchantIdentification { get; set; }

        // The customer's own description, when MerchantIdentification is Other.
        [MaxLength(1000)]
        public string? MerchantIdentificationOther { get; set; }

        // "Customer acquirer": the bank or institution that processes the payments.
        [MaxLength(200)]
        public string? CustomerAcquirer { get; set; }

        // "Type of payments using PCI Pal." Comma-separated PaymentType names.
        [MaxLength(200)]
        public string? PaymentTypes { get; set; }

        // "Currency." Comma-separated CurrencyCode names (ISO 4217).
        [MaxLength(200)]
        public string? Currencies { get; set; }

        // "Card number masking."
        public CardNumberMasking? CardNumberMasking { get; set; }

        // "SMS Service Provider": PCI Pal's Twilio account, or the customer's own.
        public SmsServiceProvider? SmsServiceProvider { get; set; }

        // "SMS Provider": the customer's own provider; only used when SmsServiceProvider is Other.
        public OtherSmsProvider? OtherSmsProvider { get; set; }

        // "Email Service Provider": PCI Pal's SendGrid account, or the customer's own.
        public EmailServiceProvider? EmailServiceProvider { get; set; }

        // The customer's own email provider; only used when EmailServiceProvider is Other (stored in OtherEmailProviderName column).
        [Column("OtherEmailProviderName")]
        public OtherEmailProvider? OtherEmailProvider { get; set; }

        // "Test or sandbox environment is available for integration testing."
        public bool HasSandboxEnvironment { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public Project Project { get; set; } = null!;
    }
}

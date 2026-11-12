using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Domain.BuildRules
{
    /// <summary>The payment gateway must be one that is marked standard.</summary>
    public sealed class StandardGatewayRule : IBuildRule
    {
        public string Code => "GATEWAY_STANDARD";
        public string Description => "The selected payment gateway is a standard gateway.";

        public string? Evaluate(BuildAssessmentInput input)
            => input.Gateway is { IsStandard: false } gateway
                ? $"The payment gateway \"{gateway.Name}\" is not a standard gateway."
                : null;

        public string? Subject(BuildAssessmentInput input) => input.Gateway?.Name;
    }

    /// <summary>Every selected gateway field must be a standard field of the gateway.</summary>
    public sealed class StandardGatewayFieldsRule : IBuildRule
    {
        public string Code => "FIELDS_STANDARD";
        public string Description => "Every selected gateway field is a standard field.";

        public string? Evaluate(BuildAssessmentInput input)
        {
            var custom = NonStandardFields(input);
            return custom.Count == 0
                ? null
                : $"Non-standard gateway field(s) selected: {string.Join(", ", custom)}.";
        }

        public string? Subject(BuildAssessmentInput input) => string.Join(", ", NonStandardFields(input));

        private static List<string> NonStandardFields(BuildAssessmentInput input)
            => input.Fields
                .Where(f => !f.IsStandard || !f.IsKnownField)
                .Select(f => f.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    /// <summary>
    /// Web callback and redirect need custom work. (The methods that make a build custom are listed
    /// here, in one place, so they are easy to change.)
    /// </summary>
    public sealed class PostTransactionMethodRule : IBuildRule
    {
        public static readonly IReadOnlySet<PostTransactionMethod> CustomMethods = new HashSet<PostTransactionMethod>
        {
            PostTransactionMethod.WebCallback,
            PostTransactionMethod.Redirect
        };

        public string Code => "POST_TRANSACTION_STANDARD";
        public string Description => "Post-transaction data transfer does not use web callback or redirect.";

        public string? Evaluate(BuildAssessmentInput input)
        {
            var custom = CustomMethodsChosen(input);
            return custom.Count == 0
                ? null
                : $"Post-transaction method(s) that need a custom build: {string.Join(", ", custom.Select(m => m.ToString()))}.";
        }

        public string? Subject(BuildAssessmentInput input) => string.Join(", ", CustomMethodsChosen(input).Select(m => m.ToString()));

        private static List<PostTransactionMethod> CustomMethodsChosen(BuildAssessmentInput input)
            => input.PostTransactionMethods.Where(CustomMethods.Contains).Distinct().ToList();
    }

    /// <summary>A standard build has a single merchant account; more than one needs custom work.</summary>
    public sealed class SingleMerchantAccountRule : IBuildRule
    {
        public string Code => "MERCHANT_ACCOUNTS_SINGLE";
        public string Description => "There is only one merchant account.";

        public string? Evaluate(BuildAssessmentInput input)
            => input.TotalMerchantAccounts is > 1 and var accounts
                ? $"There are {accounts} merchant accounts (a standard build has one)."
                : null;

        public string? Subject(BuildAssessmentInput input) => input.TotalMerchantAccounts?.ToString();
    }

    /// <summary>A standard build handles one type of payment; more than one (for example authorization and capture / sale) needs custom work.</summary>
    public sealed class SinglePaymentTypeRule : IBuildRule
    {
        public string Code => "PAYMENT_TYPES_SINGLE";
        public string Description => "Only one type of payment is used.";

        public string? Evaluate(BuildAssessmentInput input)
            => input.PaymentTypes.Distinct().Count() > 1
                ? $"More than one type of payment is used: {Subject(input)}."
                : null;

        public string? Subject(BuildAssessmentInput input)
            => string.Join(", ", input.PaymentTypes.Distinct()
                .Select(t => ProjectOptions.PaymentTypeLabels.TryGetValue(t, out var label) ? label : t.ToString()));
    }

    /// <summary>SMS must use PCI Pal's Twilio account, or not be used at all. The customer's own SMS provider needs custom work.</summary>
    public sealed class PciPalSmsProviderRule : IBuildRule
    {
        public string Code => "SMS_PCIPAL_TWILIO";
        public string Description => "SMS uses PCI Pal's Twilio account, or is not used.";

        public string? Evaluate(BuildAssessmentInput input)
            => input.SmsProvider == SmsServiceProvider.Other
                ? "SMS is sent through the customer's own provider, not PCI Pal's Twilio account."
                : null;
    }

    /// <summary>Email must use PCI Pal's SendGrid account, or not be used at all. The customer's own email provider needs custom work.</summary>
    public sealed class PciPalEmailProviderRule : IBuildRule
    {
        public string Code => "EMAIL_PCIPAL_SENDGRID";
        public string Description => "Email uses PCI Pal's SendGrid account, or is not used.";

        public string? Evaluate(BuildAssessmentInput input)
            => input.EmailProvider == EmailServiceProvider.Other
                ? "Email is sent through the customer's own provider, not PCI Pal's SendGrid account."
                : null;
    }
}

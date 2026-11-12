using AOB.BAL.Common.Enums;

namespace AOB.BAL.Domain.BuildRules
{
    /// <summary>
    /// Everything the build rules look at, copied out of the project so the rules stay plain code that
    /// can be tested without a database.
    /// </summary>
    public sealed class BuildAssessmentInput
    {
        /// <summary>The chosen payment gateway, or null when none was chosen.</summary>
        public GatewayChoice? Gateway { get; init; }

        /// <summary>The gateway fields included in the integration.</summary>
        public IReadOnlyList<FieldChoice> Fields { get; init; } = Array.Empty<FieldChoice>();

        /// <summary>How results come back after a transaction (Adaptive configuration).</summary>
        public IReadOnlyCollection<PostTransactionMethod> PostTransactionMethods { get; init; } = Array.Empty<PostTransactionMethod>();

        /// <summary>The total number of merchant accounts, or null when not answered.</summary>
        public int? TotalMerchantAccounts { get; init; }

        /// <summary>The types of payment chosen (authorization, capture / sale, tokenization).</summary>
        public IReadOnlyCollection<PaymentType> PaymentTypes { get; init; } = Array.Empty<PaymentType>();

        public SmsServiceProvider? SmsProvider { get; init; }

        public EmailServiceProvider? EmailProvider { get; init; }

        public sealed record GatewayChoice(string Name, bool IsStandard);

        /// <param name="IsKnownField">False when the project row no longer matches a field of the gateway.</param>
        public sealed record FieldChoice(string Name, bool IsStandard, bool IsKnownField = true);
    }
}

using AOB.BAL.Common.Enums;
using AOB.BAL.Domain.BuildRules;

namespace AOB.Tests.BuildRules
{
    /// <summary>Checks that the domain rules decide standard vs custom exactly as agreed.</summary>
    public class BuildClassifierTests
    {
        private readonly BuildClassifier _classifier = new();

        /// <summary>A project where everything is standard. Each test changes one thing.</summary>
        private static BuildAssessmentInput Standard(
            BuildAssessmentInput.GatewayChoice? gateway = null,
            IReadOnlyList<BuildAssessmentInput.FieldChoice>? fields = null,
            IReadOnlyCollection<PostTransactionMethod>? methods = null,
            SmsServiceProvider? sms = SmsServiceProvider.TwilioPciPalAccount,
            EmailServiceProvider? email = EmailServiceProvider.SendGridPciPalAccount,
            int? merchants = 1,
            IReadOnlyCollection<PaymentType>? paymentTypes = null)
            => new()
            {
                Gateway = gateway ?? new BuildAssessmentInput.GatewayChoice("Stripe", true),
                Fields = fields ?? new[]
                {
                    new BuildAssessmentInput.FieldChoice("Transaction Amount", true),
                    new BuildAssessmentInput.FieldChoice("Authorisation Code", true)
                },
                PostTransactionMethods = methods ?? new[] { PostTransactionMethod.Polling, PostTransactionMethod.PostMessage },
                TotalMerchantAccounts = merchants,
                PaymentTypes = paymentTypes ?? new[] { PaymentType.CaptureSale },
                SmsProvider = sms,
                EmailProvider = email
            };

        [Fact]
        public void Everything_standard_is_a_standard_build()
        {
            var result = _classifier.Assess(Standard());

            Assert.Equal(BuildType.Standard, result.Type);
            Assert.True(result.IsStandard);
            Assert.Empty(result.Failures);
        }

        [Fact]
        public void Non_standard_gateway_is_custom()
        {
            var result = _classifier.Assess(Standard(gateway: new BuildAssessmentInput.GatewayChoice("Acme Pay", false)));

            Assert.Equal(BuildType.Custom, result.Type);
            Assert.Contains(result.Failures, f => f.Code == "GATEWAY_STANDARD");
        }

        [Fact]
        public void A_non_standard_field_is_custom()
        {
            var fields = new[]
            {
                new BuildAssessmentInput.FieldChoice("Transaction Amount", true),
                new BuildAssessmentInput.FieldChoice("Loyalty Points", false)
            };

            var result = _classifier.Assess(Standard(fields: fields));

            Assert.Equal(BuildType.Custom, result.Type);
            var failure = Assert.Single(result.Failures);
            Assert.Equal("FIELDS_STANDARD", failure.Code);
            Assert.Contains("Loyalty Points", failure.Reason);
            Assert.DoesNotContain("Transaction Amount", failure.Reason);
        }

        [Fact]
        public void A_field_that_is_no_longer_in_the_gateway_is_custom()
        {
            var fields = new[] { new BuildAssessmentInput.FieldChoice("Old Field", true, IsKnownField: false) };

            Assert.Equal(BuildType.Custom, _classifier.Assess(Standard(fields: fields)).Type);
        }

        [Theory]
        [InlineData(PostTransactionMethod.WebCallback)]
        [InlineData(PostTransactionMethod.Redirect)]
        public void Web_callback_or_redirect_is_custom(PostTransactionMethod method)
        {
            var result = _classifier.Assess(Standard(methods: new[] { PostTransactionMethod.Polling, method }));

            Assert.Equal(BuildType.Custom, result.Type);
            Assert.Contains(result.Failures, f => f.Code == "POST_TRANSACTION_STANDARD");
        }

        [Theory]
        [InlineData(PostTransactionMethod.WebSocket)]
        [InlineData(PostTransactionMethod.Polling)]
        [InlineData(PostTransactionMethod.PostMessage)]
        public void The_other_post_transaction_methods_stay_standard(PostTransactionMethod method)
        {
            Assert.Equal(BuildType.Standard, _classifier.Assess(Standard(methods: new[] { method })).Type);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        public void More_than_one_merchant_account_is_custom(int accounts)
        {
            var result = _classifier.Assess(Standard(merchants: accounts));

            Assert.Equal(BuildType.Custom, result.Type);
            var failure = Assert.Single(result.Failures);
            Assert.Equal("MERCHANT_ACCOUNTS_SINGLE", failure.Code);
            Assert.Equal(accounts.ToString(), failure.Subject);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(null)]
        public void One_or_no_answer_for_merchant_accounts_stays_standard(int? accounts)
        {
            Assert.Equal(BuildType.Standard, _classifier.Assess(Standard(merchants: accounts)).Type);
        }

        [Fact]
        public void More_than_one_payment_type_is_custom()
        {
            var result = _classifier.Assess(Standard(paymentTypes: new[] { PaymentType.Authorization, PaymentType.CaptureSale }));

            Assert.Equal(BuildType.Custom, result.Type);
            var failure = Assert.Single(result.Failures);
            Assert.Equal("PAYMENT_TYPES_SINGLE", failure.Code);
            Assert.Equal("Authorization, Capture / Sale", failure.Subject);
        }

        [Theory]
        [InlineData(PaymentType.Authorization)]
        [InlineData(PaymentType.CaptureSale)]
        [InlineData(PaymentType.Tokenization)]
        public void A_single_payment_type_stays_standard(PaymentType type)
        {
            Assert.Equal(BuildType.Standard, _classifier.Assess(Standard(paymentTypes: new[] { type })).Type);
        }

        [Fact]
        public void No_payment_type_chosen_stays_standard()
        {
            Assert.Equal(BuildType.Standard, _classifier.Assess(Standard(paymentTypes: Array.Empty<PaymentType>())).Type);
        }

        [Fact]
        public void Own_sms_provider_is_custom()
        {
            var result = _classifier.Assess(Standard(sms: SmsServiceProvider.Other));

            Assert.Equal(BuildType.Custom, result.Type);
            Assert.Contains(result.Failures, f => f.Code == "SMS_PCIPAL_TWILIO");
        }

        [Fact]
        public void No_sms_and_no_email_are_standard()
        {
            var result = _classifier.Assess(Standard(sms: SmsServiceProvider.None, email: EmailServiceProvider.None));

            Assert.Equal(BuildType.Standard, result.Type);
        }

        [Fact]
        public void Own_email_provider_is_custom()
        {
            var result = _classifier.Assess(Standard(email: EmailServiceProvider.Other));

            Assert.Equal(BuildType.Custom, result.Type);
            Assert.Contains(result.Failures, f => f.Code == "EMAIL_PCIPAL_SENDGRID");
        }

        [Fact]
        public void Unanswered_sms_and_email_do_not_make_a_build_custom()
        {
            Assert.Equal(BuildType.Standard, _classifier.Assess(Standard(sms: null, email: null)).Type);
        }

        [Fact]
        public void Several_problems_are_all_reported()
        {
            var input = Standard(
                gateway: new BuildAssessmentInput.GatewayChoice("Acme Pay", false),
                methods: new[] { PostTransactionMethod.WebCallback },
                sms: SmsServiceProvider.Other,
                email: EmailServiceProvider.Other);

            var result = _classifier.Assess(input);

            Assert.Equal(BuildType.Custom, result.Type);
            Assert.Equal(4, result.Failures.Count);
            Assert.Equal(4, result.Reasons.Count());
        }

        [Fact]
        public void Failures_name_what_they_are_about()
        {
            var input = Standard(
                gateway: new BuildAssessmentInput.GatewayChoice("Acme Pay", false),
                fields: new[] { new BuildAssessmentInput.FieldChoice("Loyalty Points", false) },
                methods: new[] { PostTransactionMethod.Redirect });

            var failures = _classifier.Assess(input).Failures.ToDictionary(f => f.Code, f => f.Subject);

            Assert.Equal("Acme Pay", failures["GATEWAY_STANDARD"]);
            Assert.Equal("Loyalty Points", failures["FIELDS_STANDARD"]);
            Assert.Equal("Redirect", failures["POST_TRANSACTION_STANDARD"]);
        }

        [Fact]
        public void Rules_can_be_replaced_or_removed()
        {
            // Without the SMS rule an own SMS provider no longer matters; the rest still count.
            var rules = BuildRuleSet.Default().Where(r => r.Code != "SMS_PCIPAL_TWILIO");
            var classifier = new BuildClassifier(rules);

            Assert.Equal(BuildType.Standard, classifier.Assess(Standard(sms: SmsServiceProvider.Other)).Type);
            Assert.Equal(BuildType.Custom, classifier.Assess(Standard(email: EmailServiceProvider.Other)).Type);
        }

        [Fact]
        public void A_new_rule_can_be_added()
        {
            var classifier = new BuildClassifier(BuildRuleSet.Default().Append(new AlwaysCustomRule()));

            var result = classifier.Assess(Standard());

            Assert.Equal(BuildType.Custom, result.Type);
            Assert.Contains(result.Failures, f => f.Code == "ALWAYS");
        }

        [Fact]
        public void Every_rule_has_a_unique_code_and_a_description()
        {
            var rules = BuildRuleSet.Default();

            Assert.Equal(rules.Count, rules.Select(r => r.Code).Distinct().Count());
            Assert.All(rules, r =>
            {
                Assert.False(string.IsNullOrWhiteSpace(r.Code));
                Assert.False(string.IsNullOrWhiteSpace(r.Description));
            });
        }

        private sealed class AlwaysCustomRule : IBuildRule
        {
            public string Code => "ALWAYS";
            public string Description => "Test rule that always fails.";
            public string? Evaluate(BuildAssessmentInput input) => "Test reason.";
        }
    }
}

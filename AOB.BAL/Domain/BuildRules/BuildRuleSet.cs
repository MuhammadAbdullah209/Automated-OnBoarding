namespace AOB.BAL.Domain.BuildRules
{
    /// <summary>
    /// The domain rules that decide standard vs custom. This is the one place that lists them: add a
    /// rule here to make it count, remove one to stop it counting.
    /// </summary>
    public static class BuildRuleSet
    {
        public static IReadOnlyList<IBuildRule> Default() => new IBuildRule[]
        {
            new StandardGatewayRule(),
            new StandardGatewayFieldsRule(),
            new PostTransactionMethodRule(),
            new SingleMerchantAccountRule(),
            new SinglePaymentTypeRule(),
            new PciPalSmsProviderRule(),
            new PciPalEmailProviderRule()
        };
    }
}

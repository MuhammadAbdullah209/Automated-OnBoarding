namespace AOB.BAL.Domain.BuildRules
{
    /// <summary>
    /// One rule that decides whether a project can be a standard build. A rule either passes or
    /// reports why the project needs custom work. To change what counts as standard, edit a rule,
    /// add a new class implementing this interface, or remove one; rules are registered in
    /// <see cref="BuildRuleSet"/>.
    /// </summary>
    public interface IBuildRule
    {
        /// <summary>Stable short code, e.g. "GATEWAY_STANDARD". Stored with the result and safe to log.</summary>
        string Code { get; }

        /// <summary>What the rule checks, in plain words.</summary>
        string Description { get; }

        /// <summary>Returns null when the project satisfies the rule, otherwise the reason it needs a custom build.</summary>
        string? Evaluate(BuildAssessmentInput input);

        /// <summary>
        /// What the rule is about in plain words (the gateway name, the field names, the methods), used to ask the
        /// customer for the right information. Null when there is nothing specific to name.
        /// </summary>
        string? Subject(BuildAssessmentInput input) => null;
    }
}

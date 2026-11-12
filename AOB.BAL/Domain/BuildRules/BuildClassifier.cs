using AOB.BAL.Common.Enums;

namespace AOB.BAL.Domain.BuildRules
{
    /// <summary>The result of checking a project against the build rules.</summary>
    public sealed class BuildAssessment
    {
        public BuildType Type { get; init; }

        /// <summary>Why the build is custom; empty for a standard build.</summary>
        public IReadOnlyList<BuildRuleFailure> Failures { get; init; } = Array.Empty<BuildRuleFailure>();

        public bool IsStandard => Type == BuildType.Standard;

        public IEnumerable<string> Reasons => Failures.Select(f => f.Reason);
    }

    /// <param name="Subject">What the failing rule is about (e.g. the gateway name); null if nothing specific.</param>
    public sealed record BuildRuleFailure(string Code, string Reason, string? Subject = null);

    /// <summary>
    /// Runs every rule against a project. The build is standard only when every rule passes;
    /// a single failing rule makes it custom.
    /// </summary>
    public sealed class BuildClassifier(IEnumerable<IBuildRule> rules)
    {
        private readonly IReadOnlyList<IBuildRule> _rules = rules.ToList();

        public BuildClassifier() : this(BuildRuleSet.Default())
        {
        }

        public IReadOnlyList<IBuildRule> Rules => _rules;

        public BuildAssessment Assess(BuildAssessmentInput input)
        {
            var failures = new List<BuildRuleFailure>();
            foreach (var rule in _rules)
            {
                if (rule.Evaluate(input) is { } reason)
                {
                    failures.Add(new BuildRuleFailure(rule.Code, reason, rule.Subject(input)));
                }
            }

            return new BuildAssessment
            {
                Type = failures.Count == 0 ? BuildType.Standard : BuildType.Custom,
                Failures = failures
            };
        }
    }
}

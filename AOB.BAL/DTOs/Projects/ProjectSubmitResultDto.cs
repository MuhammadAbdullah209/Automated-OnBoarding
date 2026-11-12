using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>What happened when a project was submitted: how the build was classified and where the project went.</summary>
    public class ProjectSubmitResultDto
    {
        public BuildType BuildType { get; set; }

        /// <summary>Why the build is custom; empty for a standard build.</summary>
        public IReadOnlyList<string> Reasons { get; set; } = Array.Empty<string>();

        /// <summary>The status the project was moved to.</summary>
        public ProjectStatus Status { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace AOB.WEB.ViewModels.Projects
{
    /// <summary>Confirmation screen before a project deletion countdown starts.</summary>
    public class DeleteProjectViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int GracePeriodMinutes { get; set; }

        [Required(ErrorMessage = "Enter a reason for deleting this project.")]
        [StringLength(1000, ErrorMessage = "The reason must be 1000 characters or fewer.")]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Posted by the confirmation checkbox. Checked only when the value is true.</summary>
        public bool Confirmed { get; set; }
    }

    /// <summary>The countdown page shown while a project is waiting to be deleted.</summary>
    public class PendingDeletionViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Reason { get; set; }

        public DateTimeOffset DeleteAt { get; set; }

        /// <summary>False once the countdown has finished, and for internal staff who cannot restore.</summary>
        public bool CanRestore { get; set; }

        public string ListUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sent when someone opens a project that is already scheduled for deletion.
    /// The page sends Back to the list and every other visit to the countdown.
    /// </summary>
    public class ProjectLeaveViewModel
    {
        public string ListUrl { get; set; } = string.Empty;

        public string ContinueUrl { get; set; } = string.Empty;
    }
}

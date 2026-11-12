using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.Common.Helpers
{
    public readonly record struct CustomerHomeQueueItem(
        ProjectSummaryDto Project,
        string Title,
        string Detail,
        string Tone,
        string Icon,
        string ActionLabel,
        string ActionName);

    /// <summary>Builds the customer home “Your next steps” queue (prototype parity).</summary>
    public static class CustomerHomeQueue
    {
        /// <summary>Maximum queue rows on the customer home page.</summary>
        public const int MaxQueueItems = 10;

        /// <summary>Projects loaded when building that queue (matches <see cref="MaxQueueItems"/>).</summary>
        public const int QueueProjectFetchSize = MaxQueueItems;

        /// <summary>
        /// All four setup sections are answered but the project is not submitted yet. Such a project's button is
        /// "Readiness check" (review and submit), on the home page and in the Projects list alike, so it opens
        /// the same way from either place.
        /// </summary>
        public static bool IsReadyToSubmit(ProjectSummaryDto p)
            => p.DeletionScheduledFor is null
               && p.Status is ProjectStatus.InProgress or ProjectStatus.Draft
               && ProjectOnboardingPresentation.SetupSectionsComplete(p.CurrentStep) >= 4;

        public static IReadOnlyList<CustomerHomeQueueItem> Build(IEnumerable<ProjectSummaryDto> projects)
        {
            var items = new List<CustomerHomeQueueItem>();

            foreach (var p in projects)
            {
                if (p.DeletionScheduledFor is not null)
                {
                    continue;
                }

                var court = ProjectOnboardingPresentation.Court(p.Status, p.CurrentStep, p.InformationRequestStatus, p.AwaitingUsersCsv);

                if (p.InformationRequestStatus == InformationRequestStatus.Requested)
                {
                    items.Add(new(p,
                        "Answer PCI Pal's questions",
                        court.Text,
                        "you", "message", "Respond", "ProvideInformation"));
                    continue;
                }

                if (p.AwaitingUsersCsv && p.Status == ProjectStatus.AccountCreation)
                {
                    items.Add(new(p,
                        "Upload your users list",
                        "PCI Pal needs a CSV of names, emails and roles to create your account",
                        "you", "edit", "Upload users", "Details"));
                    continue;
                }

                if (p.Status is ProjectStatus.InProgress or ProjectStatus.Draft)
                {
                    var done = ProjectOnboardingPresentation.SetupSectionsComplete(p.CurrentStep) >= 4;
                    items.Add(new(p,
                        done ? "Submit for setup" : "Finish setup questions",
                        court.Text,
                        "you",
                        done ? "send" : "edit",
                        done ? "Review and submit" : "Continue",
                        done ? "Readiness" : ProjectOnboardingPresentation.WorkspaceAction(p.CurrentStep)));
                    continue;
                }

                if (p.Status == ProjectStatus.Completed)
                {
                    items.Add(new(p,
                        "Test your payment flow in staging",
                        p.RequiresAccount
                            ? "Launch a test payment in staging"
                            : "Download the Setup Document Pack, then launch a test",
                        "ok", "play", "Open", "Details"));
                }
            }

            static int Order(string tone) => tone switch { "bad" => 0, "you" => 1, _ => 2 };

            return items
                .OrderBy(i => Order(i.Tone))
                .ThenByDescending(i => i.Project.CreatedAt)
                .Take(MaxQueueItems)
                .ToList();
        }

        public static int ThingsNeedingYou(IReadOnlyList<CustomerHomeQueueItem> queue)
            => queue.Count(i => i.Tone is "you" or "bad");
    }
}

using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Common.Helpers
{
    public readonly record struct CourtBadgeModel(string Kind, string Who, string Text);

    public readonly record struct JourneySegment(string Label, string? StateClass, string? Subtext);

    /// <summary>Maps real project state to prototype court badges and five-stage journey meters.</summary>
    public static class ProjectOnboardingPresentation
    {
        /// <summary>The Status page shows this many activity events; "Show all" opens the Activity page.</summary>
        public const int ActivityPreviewCount = 5;

        public static int SetupSectionsComplete(ProjectStep step) => step switch
        {
            ProjectStep.Discovery => 0,
            ProjectStep.TechnicalEnvironment => 1,
            ProjectStep.PaymentConfiguration => 2,
            ProjectStep.GatewayConfiguration => 3,
            _ => 4
        };

        public static CourtBadgeModel Court(
            ProjectStatus status, ProjectStep step, InformationRequestStatus? informationRequest, bool awaitingUsersCsv = false)
        {
            if (informationRequest == InformationRequestStatus.Requested)
            {
                return new CourtBadgeModel("you", "You", "Respond to PCI Pal’s information request");
            }

            // PCI Pal is waiting for the users list before it can create the account.
            if (awaitingUsersCsv && status == ProjectStatus.AccountCreation)
            {
                return new CourtBadgeModel("you", "You", "Upload your users list");
            }

            var done = SetupSectionsComplete(step);
            return status switch
            {
                ProjectStatus.InProgress => new("you", "You", $"Finish setup · {done} of 4 sections"),
                ProjectStatus.Draft => new("you", "You", done >= 4 ? "Ready to submit" : $"Draft · {done} of 4 sections"),
                ProjectStatus.Submitted => new("pp", "PCI Pal", "Reviewing your requirements"),
                ProjectStatus.Approval => new("pp", "PCI Pal", "Awaiting approval"),
                ProjectStatus.Provisioning => new("sys", "System", "Setting up your tenant"),
                ProjectStatus.AccountCreation => new("sys", "System", "Creating your account"),
                ProjectStatus.Completed => new("ok", "Live", "Live in staging · test your flow"),
                _ => new("closed", "—", ProjectOptions.ProjectStatusLabels.TryGetValue(status, out var l) ? l : status.ToString())
            };
        }

        public static IReadOnlyList<JourneySegment> Journey(
            ProjectStatus status,
            ProjectStep step,
            InformationRequestStatus? informationRequest,
            DateTimeOffset createdAt,
            DateTimeOffset? submittedAt = null,
            BuildType? buildType = null,
            bool hasAccountStep = false,
            bool awaitingUsersCsv = false)
        {
            // When PCI Pal hosts the payment page an account is created after provisioning, so it is a stage of its own.
            var list = new List<JourneySegment>
            {
                new("Prepare", null, null),
                new("Submit", null, null),
                new("Review", null, null),
                new("Provision", null, null)
            };
            if (hasAccountStep)
            {
                list.Add(new JourneySegment("Account", null, null));
            }

            list.Add(new JourneySegment("Live", null, null));
            var live = list.Count - 1;

            if (status is ProjectStatus.InProgress or ProjectStatus.Draft)
            {
                var done = SetupSectionsComplete(step);
                list[0] = list[0] with { StateClass = "cur", Subtext = $"{done} of 4" };
                return list;
            }

            list[0] = list[0] with { StateClass = "done" };
            list[1] = list[1] with
            {
                StateClass = "done",
                Subtext = (submittedAt ?? createdAt).ToString("d MMM yyyy")
            };

            if (informationRequest == InformationRequestStatus.Requested)
            {
                list[2] = list[2] with { StateClass = "you", Subtext = "Needs you" };
                return list;
            }

            if (status is ProjectStatus.Submitted or ProjectStatus.Approval)
            {
                list[2] = list[2] with
                {
                    StateClass = "wait",
                    Subtext = ProjectOptions.ProjectStatusLabels[status]
                };
                return list;
            }

            list[2] = list[2] with
            {
                StateClass = "done",
                Subtext = buildType == BuildType.Standard ? "Standard" : "Approved"
            };

            if (status == ProjectStatus.Provisioning)
            {
                list[3] = list[3] with { StateClass = "cur", Subtext = "In progress" };
                return list;
            }

            if (status == ProjectStatus.AccountCreation)
            {
                if (!hasAccountStep)
                {
                    list[3] = list[3] with { StateClass = "cur", Subtext = "Account" };
                    return list;
                }

                list[3] = list[3] with { StateClass = "done" };
                list[4] = awaitingUsersCsv
                    ? list[4] with { StateClass = "you", Subtext = "Needs you" }
                    : list[4] with { StateClass = "cur", Subtext = "In progress" };
                return list;
            }

            if (status == ProjectStatus.Completed)
            {
                list[3] = list[3] with { StateClass = "done" };
                if (hasAccountStep)
                {
                    list[4] = list[4] with { StateClass = "done" };
                }

                list[live] = list[live] with { StateClass = "live", Subtext = "Staging" };
            }

            return list;
        }

        public static bool MatchesListView(
            ProjectStatus status, InformationRequestStatus? informationRequest, ProjectListView view, bool awaitingUsersCsv = false)
        {
            if (view == ProjectListView.All)
            {
                return true;
            }

            if (view == ProjectListView.NeedsYou)
            {
                return status is ProjectStatus.InProgress or ProjectStatus.Draft
                    || informationRequest == InformationRequestStatus.Requested
                    || (awaitingUsersCsv && status == ProjectStatus.AccountCreation);
            }

            if (view == ProjectListView.WithPciPal)
            {
                return status is ProjectStatus.Submitted or ProjectStatus.Approval
                    or ProjectStatus.Provisioning or ProjectStatus.AccountCreation;
            }

            if (view == ProjectListView.BeingPrepared)
            {
                return status is ProjectStatus.InProgress or ProjectStatus.Draft;
            }

            if (view == ProjectListView.Live)
            {
                return status == ProjectStatus.Completed;
            }

            return true;
        }

        public static string ShortReference(Guid projectId)
        {
            var digits = new string(projectId.ToString("N").Where(char.IsDigit).ToArray());
            if (digits.Length == 0)
            {
                digits = "0";
            }

            var tail = digits.Length <= 4 ? digits.PadLeft(4, '0') : digits[^4..];
            return "ONB-" + tail;
        }

        public static string WorkspaceAction(ProjectStep step) => step switch
        {
            ProjectStep.TechnicalEnvironment => "TechnicalEnvironment",
            ProjectStep.PaymentConfiguration => "PaymentConfiguration",
            ProjectStep.GatewayConfiguration => "GatewayConfiguration",
            ProjectStep.ReadinessCheck => "Readiness",
            _ => "Discovery"
        };

        public static string ActionLabel(
            ProjectStatus status, ProjectStep step, InformationRequestStatus? informationRequest, bool awaitingUsersCsv = false)
        {
            if (informationRequest == InformationRequestStatus.Requested)
            {
                return "Respond";
            }

            if (awaitingUsersCsv && status == ProjectStatus.AccountCreation)
            {
                return "Upload users";
            }

            return status switch
            {
                ProjectStatus.InProgress => "Continue",
                ProjectStatus.Draft => SetupSectionsComplete(step) >= 4 ? "Submit" : "Continue",
                ProjectStatus.Completed => "Open",
                _ => "View"
            };
        }
    }
}

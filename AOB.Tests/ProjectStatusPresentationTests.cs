using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Entities;
using Xunit;

namespace AOB.Tests
{
    public class ProjectStatusPresentationTests
    {
        private static Project ProjectWithRequests(params InformationRequestStatus[] statuses)
        {
            var project = new Project();
            var at = DateTimeOffset.UtcNow;
            foreach (var status in statuses)
            {
                project.InformationRequests.Add(new ProjectInformationRequest { Status = status, CreatedAt = at });
                at = at.AddMinutes(1);
            }

            return project;
        }

        [Fact]
        public void An_older_request_still_waiting_on_the_customer_wins_over_a_newer_answered_one()
        {
            var project = ProjectWithRequests(InformationRequestStatus.Requested, InformationRequestStatus.Submitted);

            Assert.Equal(InformationRequestStatus.Requested, project.InformationRequestStatus);
        }

        [Fact]
        public void Information_request_status_is_review_then_approved_then_none()
        {
            Assert.Equal(InformationRequestStatus.Submitted,
                ProjectWithRequests(InformationRequestStatus.Approved, InformationRequestStatus.Submitted).InformationRequestStatus);
            Assert.Equal(InformationRequestStatus.Approved,
                ProjectWithRequests(InformationRequestStatus.Approved, InformationRequestStatus.Approved).InformationRequestStatus);
            Assert.Null(ProjectWithRequests().InformationRequestStatus);
        }

        [Fact]
        public void Account_creation_waiting_for_the_users_csv_is_the_customers_turn()
        {
            var waiting = ProjectOnboardingPresentation.Court(
                ProjectStatus.AccountCreation, ProjectStep.ReadinessCheck, null, awaitingUsersCsv: true);
            var notWaiting = ProjectOnboardingPresentation.Court(
                ProjectStatus.AccountCreation, ProjectStep.ReadinessCheck, null);

            Assert.Equal("you", waiting.Kind);
            Assert.Equal("sys", notWaiting.Kind);
            Assert.Equal("Upload users", ProjectOnboardingPresentation.ActionLabel(
                ProjectStatus.AccountCreation, ProjectStep.ReadinessCheck, null, awaitingUsersCsv: true));
        }

        [Fact]
        public void A_hosted_project_has_an_account_stage_between_provision_and_live()
        {
            var at = DateTimeOffset.UtcNow;
            var accounting = ProjectOnboardingPresentation.Journey(
                ProjectStatus.AccountCreation, ProjectStep.ReadinessCheck, null, at, hasAccountStep: true);
            var done = ProjectOnboardingPresentation.Journey(
                ProjectStatus.Completed, ProjectStep.ReadinessCheck, null, at, hasAccountStep: true);
            var embedded = ProjectOnboardingPresentation.Journey(
                ProjectStatus.Completed, ProjectStep.ReadinessCheck, null, at);

            Assert.Equal(["Prepare", "Submit", "Review", "Provision", "Account", "Live"], accounting.Select(s => s.Label));
            Assert.Equal("done", accounting[3].StateClass);
            Assert.Equal("cur", accounting[4].StateClass);
            Assert.Equal("done", done[4].StateClass);
            Assert.Equal("live", done[5].StateClass);
            Assert.Equal(5, embedded.Count);
        }

        [Fact]
        public void The_users_csv_request_only_counts_in_account_creation()
        {
            Assert.True(ProjectOnboardingPresentation.MatchesListView(
                ProjectStatus.AccountCreation, null, ProjectListView.NeedsYou, awaitingUsersCsv: true));
            Assert.False(ProjectOnboardingPresentation.MatchesListView(
                ProjectStatus.AccountCreation, null, ProjectListView.NeedsYou));
            Assert.False(ProjectOnboardingPresentation.MatchesListView(
                ProjectStatus.Completed, null, ProjectListView.NeedsYou, awaitingUsersCsv: true));
        }
    }
}

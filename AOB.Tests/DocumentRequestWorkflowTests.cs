using System.Text;
using AOB.BAL.Common.Enums;
using AOB.BAL.Domain.BuildRules;
using AOB.BAL.DTOs.Activity;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Services;
using AOB.DAL.Implementations.Repositories;
using AOB.DAL.Implementations.Services;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AOB.Tests
{
    /// <summary>
    /// The document request workflow end to end on an in-memory database: one request per document, independent
    /// responses and reviews, resubmission on the same request, and new requests only while the project is in approval.
    /// </summary>
    public class DocumentRequestWorkflowTests : IDisposable
    {
        private readonly Guid _customerId = Guid.NewGuid();
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly OnboardingDbContext _context;
        private readonly ProjectInformationService _service;

        public DocumentRequestWorkflowTests()
        {
            _context = new OnboardingDbContext(new DbContextOptionsBuilder<OnboardingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
            _context.Projects.Add(new Project
            {
                Id = _projectId,
                CustomerId = _customerId,
                Name = "Test project",
                Status = ProjectStatus.Approval,
                BuildType = BuildType.Custom
            });
            _context.SaveChanges();

            _service = new ProjectInformationService(
                _context,
                new ProjectRepository(_context),
                new BuildClassifier(BuildRuleSet.Default().Append(new AlwaysCustomGatewayRule())),
                new NoActivityLog(),
                new StaffActor(),
                new MemoryFileStorage(),
                new NoEmail(),
                new ConfigurationBuilder().Build(),
                NullLogger<ProjectInformationService>.Instance);
        }

        public void Dispose() => _context.Dispose();

        private static InformationUploadDto Pdf(string name = "doc.pdf")
        {
            var bytes = Encoding.ASCII.GetBytes("%PDF-1.7 test");
            return new InformationUploadDto { FileName = name, ContentType = "application/pdf", Length = bytes.Length, Content = new MemoryStream(bytes) };
        }

        // Two separate requests: "Documents" (D1) and "Other" (D2), each with its own note.
        private async Task<(InformationRequestItemDto D1, InformationRequestItemDto D2)> RaiseTwoAsync()
        {
            var result = await _service.RequestAsync(_customerId, _projectId, new[]
            {
                new InformationRequestDraft("DOCUMENTS", "D1: signed agreement"),
                new InformationRequestDraft("OTHER", "D2: gateway credentials letter")
            });
            Assert.True(result.Succeeded, result.Error);
            var items = await ItemsAsync();
            return (items.Single(i => i.Code == "DOCUMENTS"), items.Single(i => i.Code == "OTHER"));
        }

        private async Task<List<InformationRequestItemDto>> ItemsAsync()
            => (await _service.GetRequestsAsync(_customerId, _projectId)).SelectMany(r => r.Items).ToList();

        private async Task<InformationRequestItemDto> ItemAsync(Guid id) => (await ItemsAsync()).Single(i => i.Id == id);

        [Fact]
        public async Task Each_document_is_its_own_request_with_its_own_note()
        {
            var (d1, d2) = await RaiseTwoAsync();

            var requests = await _service.GetRequestsAsync(_customerId, _projectId);
            Assert.Equal(2, requests.Count);
            Assert.All(requests, r => Assert.Single(r.Items));
            Assert.NotEqual(d1.RequestId, d2.RequestId);
            Assert.Equal("D1: signed agreement", d1.Instructions);
            Assert.Equal("D2: gateway credentials letter", d2.Instructions);
            Assert.All(new[] { d1, d2 }, i => Assert.Equal(InformationItemStatus.Pending, i.Status));
        }

        [Fact]
        public async Task Customer_responds_to_one_request_without_touching_the_other()
        {
            var (d1, d2) = await RaiseTwoAsync();

            var result = await _service.SubmitAsync(_customerId, _projectId, d2.Id, Pdf(), "Here it is");
            Assert.True(result.Succeeded, result.Error);

            Assert.Equal(InformationItemStatus.Pending, (await ItemAsync(d1.Id)).Status);
            var answered = await ItemAsync(d2.Id);
            Assert.Equal(InformationItemStatus.Submitted, answered.Status);
            Assert.True(answered.CanApprove);
            Assert.Equal("Here it is", answered.CustomerNote);
        }

        [Fact]
        public async Task Empty_response_is_refused()
        {
            var (d1, _) = await RaiseTwoAsync();
            var result = await _service.SubmitAsync(_customerId, _projectId, d1.Id, null, "   ");
            Assert.False(result.Succeeded);
            Assert.Equal(InformationItemStatus.Pending, (await ItemAsync(d1.Id)).Status);
        }

        [Fact]
        public async Task Note_only_response_cannot_be_approved_but_can_be_rejected_or_resolved()
        {
            var (d1, d2) = await RaiseTwoAsync();
            await _service.SubmitAsync(_customerId, _projectId, d1.Id, null, "We don't have this document");
            await _service.SubmitAsync(_customerId, _projectId, d2.Id, null, "Not applicable to us");

            var noteOnly = await ItemAsync(d1.Id);
            Assert.True(noteOnly.AwaitingReview);
            Assert.False(noteOnly.CanApprove);

            var approve = await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Approve, null);
            Assert.False(approve.Succeeded);

            Assert.True((await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Reject, "We need the file")).Succeeded);
            Assert.True((await _service.ReviewItemAsync(_customerId, _projectId, d2.Id, InformationReviewDecision.Resolve, null)).Succeeded);
            Assert.Equal(InformationItemStatus.Rejected, (await ItemAsync(d1.Id)).Status);
            Assert.Equal(InformationItemStatus.Resolved, (await ItemAsync(d2.Id)).Status);
        }

        [Fact]
        public async Task Reject_needs_a_reason_and_the_customer_sees_it()
        {
            var (d1, _) = await RaiseTwoAsync();
            await _service.SubmitAsync(_customerId, _projectId, d1.Id, Pdf(), null);

            Assert.False((await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Reject, " ")).Succeeded);
            Assert.True((await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Reject, "Unsigned copy")).Succeeded);

            var rejected = await ItemAsync(d1.Id);
            Assert.Equal(InformationItemStatus.Rejected, rejected.Status);
            Assert.Equal("Unsigned copy", rejected.ReviewNote);
            Assert.True(rejected.NeedsCustomer);
        }

        [Fact]
        public async Task Resubmission_reuses_the_request_keeps_history_and_leaves_others_alone()
        {
            var (d1, d2) = await RaiseTwoAsync();
            await _service.SubmitAsync(_customerId, _projectId, d2.Id, Pdf("first.pdf"), null);
            await _service.ReviewItemAsync(_customerId, _projectId, d2.Id, InformationReviewDecision.Reject, "Wrong file");

            // A note only this time.
            var result = await _service.SubmitAsync(_customerId, _projectId, d2.Id, null, "The letter is on its way");
            Assert.True(result.Succeeded, result.Error);

            var requests = await _service.GetRequestsAsync(_customerId, _projectId);
            Assert.Equal(2, requests.Count);
            var resubmitted = await ItemAsync(d2.Id);
            Assert.Equal(d2.RequestId, resubmitted.RequestId);
            Assert.Equal(InformationItemStatus.Resubmitted, resubmitted.Status);
            Assert.False(resubmitted.CanApprove);
            Assert.Null(resubmitted.ReviewNote);

            var earlier = Assert.Single(resubmitted.History);
            Assert.Equal(InformationItemStatus.Rejected, earlier.Outcome);
            Assert.Equal("Wrong file", earlier.ReviewNote);
            Assert.Equal("first.pdf", earlier.DocumentName);

            Assert.Equal(InformationItemStatus.Pending, (await ItemAsync(d1.Id)).Status);

            // Then a document: all three actions are possible again.
            await _service.ReviewItemAsync(_customerId, _projectId, d2.Id, InformationReviewDecision.Reject, "Still need the letter");
            await _service.SubmitAsync(_customerId, _projectId, d2.Id, Pdf("letter.pdf"), null);
            var withFile = await ItemAsync(d2.Id);
            Assert.True(withFile.CanApprove);
            Assert.Equal(2, withFile.History.Count);
        }

        [Fact]
        public async Task Approving_one_request_leaves_the_others_and_more_can_still_be_requested()
        {
            var (d1, d2) = await RaiseTwoAsync();
            await _service.SubmitAsync(_customerId, _projectId, d1.Id, Pdf(), null);
            await _service.SubmitAsync(_customerId, _projectId, d2.Id, Pdf(), null);

            Assert.True((await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Approve, null)).Succeeded);
            Assert.Equal(InformationItemStatus.Approved, (await ItemAsync(d1.Id)).Status);
            Assert.Equal(InformationItemStatus.Submitted, (await ItemAsync(d2.Id)).Status);

            // An approved document does not stop further requests while the project waits for approval.
            Assert.True((await _service.RequestAsync(_customerId, _projectId, new[] { new InformationRequestDraft("GATEWAY_STANDARD", null) })).Succeeded);
            Assert.Equal(3, (await ItemsAsync()).Count);

            // Existing requests carry on through their own reviews.
            Assert.True((await _service.ReviewItemAsync(_customerId, _projectId, d2.Id, InformationReviewDecision.Reject, "Expired")).Succeeded);
        }

        [Fact]
        public async Task No_requests_once_the_project_is_approved_for_provisioning()
        {
            await RaiseTwoAsync();
            var project = await _context.Projects.SingleAsync(p => p.Id == _projectId);
            project.Status = ProjectStatus.Provisioning;
            await _context.SaveChangesAsync();

            Assert.Empty(await _service.GetOptionsAsync(_customerId, _projectId));
            Assert.False((await _service.RequestAsync(_customerId, _projectId, new[] { new InformationRequestDraft("GATEWAY_STANDARD", "Late") })).Succeeded);
        }

        [Fact]
        public async Task A_document_cannot_be_requested_twice_even_with_another_note()
        {
            await RaiseTwoAsync();
            Assert.False((await _service.RequestAsync(_customerId, _projectId, new[] { new InformationRequestDraft("DOCUMENTS", "D5: proof of address") })).Succeeded);
            Assert.False((await _service.RequestAsync(_customerId, _projectId, new[] { new InformationRequestDraft("OTHER", "D6: something else") })).Succeeded);
            Assert.Equal(2, (await ItemsAsync()).Count);
        }

        [Fact]
        public async Task Only_responses_waiting_for_review_can_be_reviewed()
        {
            var (d1, _) = await RaiseTwoAsync();
            Assert.False((await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Resolve, null)).Succeeded);

            await _service.SubmitAsync(_customerId, _projectId, d1.Id, Pdf(), null);
            await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Approve, null);
            Assert.False((await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Reject, "late")).Succeeded);
            Assert.False((await _service.SubmitAsync(_customerId, _projectId, d1.Id, Pdf(), null)).Succeeded);
        }

        [Fact]
        public async Task Another_customer_cannot_respond_or_review()
        {
            var (d1, _) = await RaiseTwoAsync();
            var stranger = Guid.NewGuid();
            Assert.False((await _service.SubmitAsync(stranger, _projectId, d1.Id, Pdf(), "hi")).Succeeded);
            Assert.Empty(await _service.GetRequestsAsync(stranger, _projectId));
            Assert.Equal(InformationItemStatus.Pending, (await ItemAsync(d1.Id)).Status);
        }

        [Fact]
        public async Task Project_gate_opens_once_every_request_is_approved_or_resolved()
        {
            var (d1, d2) = await RaiseTwoAsync();
            var repository = new ProjectRepository(_context);
            Assert.True(await repository.HasOpenInformationRequestAsync(_projectId));

            await _service.SubmitAsync(_customerId, _projectId, d1.Id, Pdf(), null);
            await _service.SubmitAsync(_customerId, _projectId, d2.Id, null, "n/a");
            await _service.ReviewItemAsync(_customerId, _projectId, d1.Id, InformationReviewDecision.Approve, null);
            Assert.True(await repository.HasOpenInformationRequestAsync(_projectId));

            await _service.ReviewItemAsync(_customerId, _projectId, d2.Id, InformationReviewDecision.Resolve, "Not needed");
            Assert.False(await repository.HasOpenInformationRequestAsync(_projectId));
        }

        [Fact]
        public async Task Every_requested_document_leaves_the_checklist()
        {
            var before = (await _service.GetOptionsAsync(_customerId, _projectId)).Select(o => o.Code).ToList();
            Assert.Contains("GATEWAY_STANDARD", before);

            Assert.True((await _service.RequestAsync(_customerId, _projectId, new[]
            {
                new InformationRequestDraft("GATEWAY_STANDARD", null),
                new InformationRequestDraft("DOCUMENTS", "D1")
            })).Succeeded);

            var after = (await _service.GetOptionsAsync(_customerId, _projectId)).Select(o => o.Code).ToList();
            Assert.Equal(new[] { "OTHER" }, after);

            // Posting one again anyway (e.g. from a page left open) is refused.
            Assert.False((await _service.RequestAsync(_customerId, _projectId, new[] { new InformationRequestDraft("GATEWAY_STANDARD", "again") })).Succeeded);
        }

        [Fact]
        public async Task Every_document_on_the_checklist_can_be_requested_in_one_go()
        {
            var codes = (await _service.GetOptionsAsync(_customerId, _projectId)).Select(o => o.Code).ToList();
            var result = await _service.RequestAsync(_customerId, _projectId,
                codes.Select(c => new InformationRequestDraft(c, c == "OTHER" ? "Anything else" : null)).ToList());
            Assert.True(result.Succeeded, result.Error);

            Assert.Equal(codes.Count, (await _service.GetRequestsAsync(_customerId, _projectId)).Count);
            Assert.Empty(await _service.GetOptionsAsync(_customerId, _projectId));
        }

        [Theory]
        [InlineData(InformationRequestStatus.Requested, InformationItemStatus.Pending, InformationItemStatus.Submitted)]
        [InlineData(InformationRequestStatus.Requested, InformationItemStatus.Rejected, InformationItemStatus.Approved)]
        [InlineData(InformationRequestStatus.Submitted, InformationItemStatus.Resubmitted, InformationItemStatus.Resolved)]
        [InlineData(InformationRequestStatus.Approved, InformationItemStatus.Approved, InformationItemStatus.Resolved)]
        public void Request_status_follows_its_items(InformationRequestStatus expected, params InformationItemStatus[] items)
            => Assert.Equal(expected, ProjectInformationService.StatusFor(items));

        // Makes every test project a custom build for the gateway, so a build-rule document is on the checklist.
        private sealed class AlwaysCustomGatewayRule : IBuildRule
        {
            public string Code => "GATEWAY_STANDARD";
            public string Description => "Test rule";
            public string? Evaluate(BuildAssessmentInput input) => "Test gateway is not standard.";
        }

        private sealed class StaffActor : ICurrentActor
        {
            public Guid? UserAuthId { get; } = Guid.NewGuid();
            public string? Name => "Staff Reviewer";
            public string? Role => "InternalUser";
        }

        private sealed class NoActivityLog : IActivityLogService
        {
            public Task LogCustomerAsync(Guid customerId, string action, string? reason = null) => Task.CompletedTask;
            public Task LogProjectAsync(Guid customerId, Guid projectId, string action, string? reason = null) => Task.CompletedTask;
            public Task<IReadOnlyList<ActivityLogDto>> GetForCustomerAsync(Guid customerId, int take = 20)
                => Task.FromResult<IReadOnlyList<ActivityLogDto>>(Array.Empty<ActivityLogDto>());
            public Task<IReadOnlyList<ActivityLogDto>> GetForProjectAsync(Guid customerId, Guid projectId, int? take = null)
                => Task.FromResult<IReadOnlyList<ActivityLogDto>>(Array.Empty<ActivityLogDto>());
        }

        private sealed class NoEmail : IEmailService
        {
            public Task SendAsync(string toAddress, string subject, string htmlBody) => Task.CompletedTask;
            public Task SendTemplatedAsync(string toAddress, EmailTemplateType templateType, IDictionary<string, string> placeholders) => Task.CompletedTask;
        }

        private sealed class MemoryFileStorage : IFileStorage
        {
            public Task<string> SaveAsync(FileCategory category, Guid ownerId, string extension, Stream content)
                => Task.FromResult($"test/{Guid.NewGuid():N}{extension}");
            public Task<Stream?> OpenReadAsync(string storedPath) => Task.FromResult<Stream?>(new MemoryStream());
            public Task DeleteAsync(string storedPath) => Task.CompletedTask;
        }
    }
}

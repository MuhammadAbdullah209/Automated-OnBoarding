using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;
using AOB.BAL.Domain.BuildRules;
using AOB.BAL.DTOs.Documents;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Document requests for a project waiting for approval. What can be asked for comes from the reasons the build
    /// is custom (the domain build rules), so each request names the gateway, fields or methods concerned. Each
    /// requested document has its own lifecycle (Pending, Submitted, Rejected, Resubmitted, Approved, Resolved) and
    /// history; the customer's files are saved as project documents. New requests can be raised for as long as the
    /// project is waiting for approval. The project is only approved once every request is approved or resolved.
    /// </summary>
    public class ProjectInformationService(
        OnboardingDbContext context,
        IProjectRepository projectRepository,
        BuildClassifier buildClassifier,
        IActivityLogService activityLog,
        ICurrentActor actor,
        IFileStorage fileStorage,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ProjectInformationService> logger) : IProjectInformationService
    {
        private const string DocumentsCode = "DOCUMENTS";
        private const string OtherCode = "OTHER";

        private readonly long _maxFileSizeBytes = configuration.GetValue("FileStorage:MaxFileSizeMB", 10) * 1024L * 1024L;

        private readonly string[] _allowedExtensions =
            configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
            ?? new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };

        // The customer is never told the build is custom, so the wording says what to provide, not why.
        private static InformationRequestOptionDto Describe(string code, string? subject, string? internalReason)
        {
            var named = string.IsNullOrWhiteSpace(subject) ? string.Empty : $" ({subject})";
            var (title, text) = code switch
            {
                "GATEWAY_STANDARD" => ("Payment gateway information",
                    $"Please provide the details, and any agreement or credential documents, for your payment gateway{named}."),
                "FIELDS_STANDARD" => ("Gateway field information",
                    $"Please provide documentation for these gateway fields: {subject}."),
                "POST_TRANSACTION_STANDARD" => ("Integration details",
                    $"Please provide the technical details for how results should be returned to you{named}."),
                "MERCHANT_ACCOUNTS_SINGLE" => ("Merchant account information",
                    $"Please provide the details of your merchant accounts{named} and how they should be told apart."),
                "PAYMENT_TYPES_SINGLE" => ("Payment type information",
                    $"Please explain how each type of payment should be handled{named}."),
                "SMS_PCIPAL_TWILIO" => ("SMS provider information",
                    "Please provide the details of the SMS provider you want to use."),
                "EMAIL_PCIPAL_SENDGRID" => ("Email provider information",
                    "Please provide the details of the email provider you want to use."),
                DocumentsCode => ("Documents",
                    "Please upload any supporting documents we need to review your set-up."),
                OtherCode => ("Other",
                    "Please provide the additional information described in the note."),
                _ => ("Additional information", "Please provide the information we asked for.")
            };

            return new InformationRequestOptionDto
            {
                Code = code,
                Title = title,
                Description = text,
                InternalReason = internalReason
            };
        }

        public async Task<IReadOnlyList<InformationRequestOptionDto>> GetOptionsAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null || project.Status != ProjectStatus.Approval)
            {
                return Array.Empty<InformationRequestOptionDto>();
            }

            return await AvailableOptionsAsync(project);
        }

        // What can still be asked for: every document leaves the checklist once it has been requested, "Documents" and
        // "Other" included. Its request is then answered, rejected and resubmitted on its own, never asked for twice.
        private async Task<List<InformationRequestOptionDto>> AvailableOptionsAsync(Project project)
        {
            var requested = await context.ProjectInformationRequestItems.AsNoTracking()
                .Where(i => i.Request.ProjectId == project.Id)
                .Select(i => i.Code)
                .Distinct()
                .ToListAsync();
            return OptionsFor(project)
                .Where(o => !requested.Contains(o.Code))
                .ToList();
        }

        private List<InformationRequestOptionDto> OptionsFor(Project project)
        {
            var assessment = buildClassifier.Assess(BuildAssessmentMapper.ToInput(project));
            var options = assessment.Failures
                .Select(f => Describe(f.Code, f.Subject, f.Reason))
                .ToList();
            options.Add(Describe(DocumentsCode, null, null));
            options.Add(Describe(OtherCode, null, null));
            return options;
        }

        public async Task<ServiceResult> RequestAsync(Guid customerId, Guid projectId, IReadOnlyList<InformationRequestDraft> drafts)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }

            if (project.Status != ProjectStatus.Approval)
            {
                return ServiceResult.Failure("Documents can only be requested while the project is pending approval.");
            }

            var allOptions = OptionsFor(project);
            var options = await AvailableOptionsAsync(project);
            var chosen = new List<(InformationRequestOptionDto Option, string? Note)>();
            foreach (var draft in drafts)
            {
                var option = options.FirstOrDefault(o => o.Code == draft.Code);
                if (option is null)
                {
                    // Already requested (the page was open while someone else asked for it): never ask twice.
                    if (allOptions.FirstOrDefault(o => o.Code == draft.Code) is { } taken)
                    {
                        return ServiceResult.Failure($"\"{taken.Title}\" has already been requested. Review the existing request instead.");
                    }

                    continue;
                }

                var note = string.IsNullOrWhiteSpace(draft.Note) ? null : draft.Note.Trim();
                if (option.Code == OtherCode && note is null)
                {
                    return ServiceResult.Failure("Add a note saying what is needed for \"Other\".");
                }

                if (chosen.All(c => c.Option.Code != option.Code))
                {
                    chosen.Add((option, note));
                }
            }

            if (chosen.Count == 0)
            {
                return ServiceResult.Failure("Select at least one document to request.");
            }

            // Each document is its own request, so it is answered and reviewed on its own.
            foreach (var (option, note) in chosen)
            {
                var request = new ProjectInformationRequest
                {
                    ProjectId = projectId,
                    RequestedByUserAuthId = actor.UserAuthId,
                    RequestedByName = actor.Name
                };
                request.Items.Add(new ProjectInformationRequestItem
                {
                    Code = option.Code,
                    Title = option.Title,
                    Description = option.Description,
                    Instructions = note
                });
                context.ProjectInformationRequests.Add(request);
            }

            project.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();

            var titles = chosen.Select(c => c.Option.Title).ToList();
            await activityLog.LogProjectAsync(customerId, projectId,
                chosen.Count == 1 ? "Document requested" : "Documents requested",
                "Requested: " + string.Join(", ", titles) + ".");

            await NotifyCustomerAsync(customerId, project, titles);
            return ServiceResult.Success();
        }

        /// <summary>
        /// Emails the customer's primary contact (the login email if there is no contact email) that action is required
        /// on the project. A failure to send is logged and never stops the request.
        /// </summary>
        private async Task NotifyCustomerAsync(Guid customerId, Project project, IReadOnlyList<string> titles)
        {
            try
            {
                var customer = await context.Customers.AsNoTracking()
                    .Where(c => c.Id == customerId)
                    .Select(c => new
                    {
                        c.PrimaryContactEmail,
                        c.PrimaryContactFullName,
                        LoginEmail = c.Auth != null ? c.Auth.Email : null,
                        HasLogin = c.UserAuthId != null
                    })
                    .FirstOrDefaultAsync();

                var to = !string.IsNullOrWhiteSpace(customer?.PrimaryContactEmail) ? customer!.PrimaryContactEmail : customer?.LoginEmail;
                if (string.IsNullOrWhiteSpace(to))
                {
                    logger.LogWarning("No contact email for customer {CustomerId}; the information request for project {ProjectId} was not emailed.",
                        customerId, project.Id);
                    return;
                }

                var baseUrl = (configuration["AppSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
                // A customer with a login goes straight to the project; otherwise the link opens the portal.
                var link = customer!.HasLogin ? $"{baseUrl}/Projects/ProvideInformation/{project.Id}" : baseUrl;

                await emailService.SendTemplatedAsync(to, EmailTemplateType.InformationRequested, new Dictionary<string, string>
                {
                    ["ConsumerName"] = string.IsNullOrWhiteSpace(customer.PrimaryContactFullName) ? "there" : customer.PrimaryContactFullName,
                    ["ApplicationReference"] = project.Name,
                    // Only the item titles go in the email; the reviewer's notes stay in the portal.
                    ["InformationRequestSummary"] = string.Join(", ", titles),
                    ["ConsumerPortalLink"] = link,
                    ["SupportContact"] = string.IsNullOrWhiteSpace(configuration["AppSettings:SupportContact"])
                        ? "the PCI Pal team"
                        : configuration["AppSettings:SupportContact"]!
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not email the information request for project {ProjectId}.", project.Id);
            }
        }

        public async Task<IReadOnlyList<InformationRequestDto>> GetRequestsAsync(Guid customerId, Guid projectId)
        {
            if (!await context.Projects.AnyAsync(p => p.Id == projectId && p.CustomerId == customerId))
            {
                return Array.Empty<InformationRequestDto>();
            }

            var requests = await context.ProjectInformationRequests.AsNoTracking()
                .Include(r => r.Items).ThenInclude(i => i.Responses)
                .Where(r => r.ProjectId == projectId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var items = requests.SelectMany(r => r.Items).ToList();
            var documentIds = items.Where(i => i.DocumentId != null).Select(i => i.DocumentId!.Value)
                .Concat(items.SelectMany(i => i.Responses).Where(r => r.DocumentId != null).Select(r => r.DocumentId!.Value))
                .Distinct()
                .ToList();
            var documentNames = await context.ProjectDocuments.AsNoTracking()
                .Where(d => documentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.OriginalFileName);
            string? NameOf(Guid? id) => id is { } value && documentNames.TryGetValue(value, out var name) ? name : null;

            return requests.Select(r => new InformationRequestDto
            {
                Id = r.Id,
                Status = r.Status,
                Instructions = r.Instructions,
                RequestedByName = r.RequestedByName,
                CreatedAt = r.CreatedAt,
                SubmittedAt = r.SubmittedAt,
                CustomerNotes = r.CustomerNotes,
                Items = r.Items.OrderBy(i => i.Title).Select(i => new InformationRequestItemDto
                {
                    Id = i.Id,
                    RequestId = r.Id,
                    Code = i.Code,
                    Title = i.Title,
                    Description = i.Description,
                    // Older requests kept one note and one customer answer for all their items.
                    Instructions = i.Instructions ?? r.Instructions,
                    RequestedByName = r.RequestedByName,
                    CreatedAt = r.CreatedAt,
                    Status = i.Status,
                    ReviewNote = i.ReviewNote,
                    CustomerNote = i.CustomerNote ?? (i.Status == InformationItemStatus.Pending ? null : r.CustomerNotes),
                    SubmittedAt = i.SubmittedAt,
                    ReviewedAt = i.ReviewedAt,
                    DocumentId = i.DocumentId,
                    DocumentName = NameOf(i.DocumentId),
                    // The latest response is shown on the item itself; the history is everything before it.
                    History = i.Responses.OrderByDescending(x => x.SubmittedAt).Skip(1).Select(x => new InformationResponseDto
                    {
                        SubmittedAt = x.SubmittedAt,
                        Note = x.Note,
                        DocumentId = x.DocumentId,
                        DocumentName = NameOf(x.DocumentId),
                        Outcome = x.Outcome,
                        ReviewNote = x.ReviewNote,
                        ReviewedAt = x.ReviewedAt,
                        ReviewedByName = x.ReviewedByName
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        /// <summary>
        /// Where a request stands overall, from its items: waiting for the customer while any item is pending or
        /// rejected, waiting for review while any response is unreviewed, otherwise closed.
        /// </summary>
        public static InformationRequestStatus StatusFor(IEnumerable<InformationItemStatus> items)
        {
            var statuses = items.ToList();
            if (statuses.Any(s => s is InformationItemStatus.Pending or InformationItemStatus.Rejected))
            {
                return InformationRequestStatus.Requested;
            }

            return statuses.Any(s => s is InformationItemStatus.Submitted or InformationItemStatus.Resubmitted)
                ? InformationRequestStatus.Submitted
                : InformationRequestStatus.Approved;
        }

        public async Task<ServiceResult> SubmitAsync(Guid customerId, Guid projectId, Guid itemId, InformationUploadDto? upload, string? note)
        {
            var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.CustomerId == customerId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }

            var item = await context.ProjectInformationRequestItems
                .Include(i => i.Request).ThenInclude(r => r.Items)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Request.ProjectId == projectId);
            if (item is null)
            {
                return ServiceResult.Failure("Request not found.");
            }

            if (item.Status is not (InformationItemStatus.Pending or InformationItemStatus.Rejected))
            {
                return ServiceResult.Failure("This request is not waiting for a response from you.");
            }

            // A document, a note, or both: at least one, so a response is never empty.
            var cleanedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            string? safeName = null;
            string? extension = null;
            if (upload is { Length: > 0 })
            {
                if (upload.Length > _maxFileSizeBytes)
                {
                    return ServiceResult.Failure($"\"{upload.FileName}\" is too large. The limit is {_maxFileSizeBytes / 1024 / 1024} MB.");
                }

                safeName = Path.GetFileName(upload.FileName);
                extension = Path.GetExtension(safeName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    return ServiceResult.Failure($"\"{safeName}\" is not an allowed file type. Allowed: {string.Join(", ", _allowedExtensions)}.");
                }

                if (!await AOB.DAL.Common.FileSignatureValidator.MatchesExtensionAsync(extension, upload.Content))
                {
                    return ServiceResult.Failure($"\"{safeName}\" does not look like a valid {extension} file.");
                }
            }

            if (safeName is null && cleanedNote is null)
            {
                return ServiceResult.Failure("Upload a document, or write a note, before submitting.");
            }

            var wasRejected = item.Status == InformationItemStatus.Rejected;
            var now = DateTimeOffset.UtcNow;
            string? storedPath = null;
            try
            {
                ProjectDocument? document = null;
                if (upload is not null && safeName is not null && extension is not null)
                {
                    storedPath = await fileStorage.SaveAsync(FileCategory.ProjectDocument, projectId, extension, upload.Content);
                    document = new ProjectDocument
                    {
                        ProjectId = projectId,
                        Type = ProjectDocumentType.InformationRequest,
                        OriginalFileName = safeName,
                        StoredFilePath = storedPath,
                        ContentType = AOB.DAL.Common.FileSignatureValidator.ContentTypeFor(extension),
                        FileSizeBytes = upload.Length,
                        InformationRequestItemId = item.Id
                    };
                    context.ProjectDocuments.Add(document);
                }

                // Earlier responses (and their files) stay on record; the item shows this one.
                context.ProjectInformationResponses.Add(new ProjectInformationResponse
                {
                    ItemId = item.Id,
                    Note = cleanedNote,
                    DocumentId = document?.Id,
                    SubmittedAt = now
                });

                item.Status = wasRejected ? InformationItemStatus.Resubmitted : InformationItemStatus.Submitted;
                item.DocumentId = document?.Id;
                item.CustomerNote = cleanedNote;
                item.ReviewNote = null;
                item.SubmittedAt = now;
                item.ReviewedAt = null;

                var request = item.Request;
                request.Status = StatusFor(request.Items.Select(i => i.Status));
                request.SubmittedAt = now;
                request.CustomerRespondedAt = now;
                project.UpdatedAt = now;
                await context.SaveChangesAsync();
            }
            catch
            {
                // The save failed, so do not leave the new file behind.
                if (storedPath is not null)
                {
                    try { await fileStorage.DeleteAsync(storedPath); }
                    catch (Exception ex) { logger.LogWarning(ex, "Could not delete file {Path}.", storedPath); }
                }

                throw;
            }

            var detail = new List<string> { $"{item.Title}." };
            if (safeName is not null)
            {
                detail.Add($"File: {safeName}.");
            }
            if (cleanedNote is not null)
            {
                detail.Add("Note: " + cleanedNote);
            }
            await activityLog.LogProjectAsync(customerId, projectId,
                wasRejected ? "Document resubmitted" : "Document submitted",
                string.Join(" ", detail));
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ReviewItemAsync(Guid customerId, Guid projectId, Guid itemId, InformationReviewDecision decision, string? note)
        {
            var item = await context.ProjectInformationRequestItems
                .Include(i => i.Request).ThenInclude(r => r.Items)
                .Include(i => i.Responses)
                .FirstOrDefaultAsync(i => i.Id == itemId
                                          && i.Request.ProjectId == projectId
                                          && i.Request.Project.CustomerId == customerId);
            if (item is null)
            {
                return ServiceResult.Failure("Request not found.");
            }

            if (item.Status is not (InformationItemStatus.Submitted or InformationItemStatus.Resubmitted))
            {
                return ServiceResult.Failure("Only a response waiting for review can be approved, rejected or resolved.");
            }

            var cleanedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            switch (decision)
            {
                case InformationReviewDecision.Approve when item.DocumentId is null:
                    return ServiceResult.Failure("There is no document to approve. Reject or resolve this request instead.");
                case InformationReviewDecision.Reject when cleanedNote is null:
                    return ServiceResult.Failure("Give a reason for rejecting this response.");
                case InformationReviewDecision.Approve or InformationReviewDecision.Reject or InformationReviewDecision.Resolve:
                    break;
                default:
                    return ServiceResult.Failure("Choose whether to approve, reject or resolve.");
            }

            var now = DateTimeOffset.UtcNow;
            item.Status = decision switch
            {
                InformationReviewDecision.Approve => InformationItemStatus.Approved,
                InformationReviewDecision.Reject => InformationItemStatus.Rejected,
                _ => InformationItemStatus.Resolved
            };
            item.ReviewNote = decision == InformationReviewDecision.Approve ? null : cleanedNote;
            item.ReviewedAt = now;

            // Record the decision on the response it was made about, so the history keeps it after a resubmission.
            var latest = item.Responses.OrderByDescending(r => r.SubmittedAt).FirstOrDefault();
            if (latest is not null && latest.Outcome is null)
            {
                latest.Outcome = item.Status;
                latest.ReviewNote = item.ReviewNote;
                latest.ReviewedAt = now;
                latest.ReviewedByName = actor.Name;
            }

            item.Request.Status = StatusFor(item.Request.Items.Select(i => i.Status));
            await context.SaveChangesAsync();

            var (activityTitle, activityDetail) = decision switch
            {
                InformationReviewDecision.Approve => ("Document approved", $"{item.Title} was approved."),
                InformationReviewDecision.Reject => ("Document rejected", $"{item.Title}: {cleanedNote}"),
                _ => ("Document request resolved",
                    cleanedNote is null ? $"{item.Title} was resolved." : $"{item.Title} was resolved: {cleanedNote}")
            };
            await activityLog.LogProjectAsync(customerId, projectId, activityTitle, activityDetail);

            // A rejection is an action for the customer: email their primary contact, as the first request does.
            if (decision == InformationReviewDecision.Reject)
            {
                var project = await context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId);
                if (project is not null)
                {
                    await NotifyCustomerAsync(customerId, project, new[] { item.Title });
                }
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<DocumentFileDto>> DownloadAsync(Guid customerId, Guid projectId, Guid documentId)
        {
            var document = await context.ProjectDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId
                                          && d.ProjectId == projectId
                                          && d.Project.CustomerId == customerId
                                          && d.InformationRequestItemId != null);
            if (document is null)
            {
                return ServiceResult<DocumentFileDto>.Failure("Document not found.");
            }

            var stream = await fileStorage.OpenReadAsync(document.StoredFilePath);
            if (stream is null)
            {
                return ServiceResult<DocumentFileDto>.Failure("The file is no longer available.");
            }

            return ServiceResult<DocumentFileDto>.Success(new DocumentFileDto
            {
                Content = stream,
                FileName = document.OriginalFileName,
                ContentType = document.ContentType ?? "application/octet-stream"
            });
        }
    }
}

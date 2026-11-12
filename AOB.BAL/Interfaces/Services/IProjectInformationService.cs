using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Documents;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>
    /// Document requests on a project waiting for approval (a custom build). An internal user asks for documents one
    /// by one, each with an optional note. The customer responds to each on its own (a file, a note, or both), and the
    /// internal user approves, rejects or resolves each response. A rejected request is answered again on the same
    /// request. New requests can be raised for as long as the project is waiting for approval. The project can only be
    /// approved once every request is approved or resolved.
    /// </summary>
    public interface IProjectInformationService
    {
        /// <summary>
        /// The documents that can still be requested for this project: one for each reason the build is custom, plus
        /// "Documents" and "Other", leaving out any already requested. Empty unless the project is waiting for approval.
        /// </summary>
        Task<IReadOnlyList<InformationRequestOptionDto>> GetOptionsAsync(Guid customerId, Guid projectId);

        /// <summary>
        /// Raises one request per document, as many as needed while the project is waiting for approval. Fails when a
        /// document was already requested.
        /// </summary>
        Task<ServiceResult> RequestAsync(Guid customerId, Guid projectId, IReadOnlyList<InformationRequestDraft> drafts);

        /// <summary>Every request for the project, newest first.</summary>
        Task<IReadOnlyList<InformationRequestDto>> GetRequestsAsync(Guid customerId, Guid projectId);

        /// <summary>
        /// The customer's response to one requested document: a file, a note, or both. Allowed while the request is
        /// pending, or after it was rejected (it then becomes Resubmitted). Other requests are not affected.
        /// </summary>
        Task<ServiceResult> SubmitAsync(Guid customerId, Guid projectId, Guid itemId, InformationUploadDto? upload, string? note);

        /// <summary>
        /// Approves, rejects (a reason is needed) or resolves the customer's latest response to one document.
        /// Approve needs a document on the response.
        /// </summary>
        Task<ServiceResult> ReviewItemAsync(Guid customerId, Guid projectId, Guid itemId, InformationReviewDecision decision, string? note);

        /// <summary>Opens a file the customer uploaded for a request.</summary>
        Task<ServiceResult<DocumentFileDto>> DownloadAsync(Guid customerId, Guid projectId, Guid documentId);
    }
}

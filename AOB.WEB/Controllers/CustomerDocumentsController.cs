using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.Controllers
{
    /// <summary>
    /// The signed-in customer's Documents / Evidence page. The customer is always taken from the
    /// sign-in claim, so a customer can only see, upload and download their own documents.
    /// </summary>
    [Authorize(Roles = nameof(AOB.BAL.Common.Enums.UserType.Customer))]
    public class CustomerDocumentsController(
        ICustomerDocumentService customerDocumentService,
        ICustomerService customerService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customerId = GetCustomerId();
            var documents = await customerDocumentService.GetChecklistAsync(customerId);
            var profileResult = await customerService.GetProfileAsync(customerId);
            var profile = profileResult.Data ?? new();

            ViewData["Title"] = "Organisation";
            ViewData["FullBleed"] = true;
            ViewData["OrgTab"] = "docs";
            ViewData["OrgBandCompany"] = string.IsNullOrWhiteSpace(profile.CompanyName)
                ? "Your organisation"
                : profile.CompanyName;
            var okN = CustomerProfileCompletionRules.RequiredProfileSectionsComplete(profile);
            ViewData["OrgSectionsComplete"] = okN;
            ViewData["OrgDocumentsUploaded"] = documents.Count(d => d.IsUploaded);
            ViewData["OrgDocumentsTotal"] = documents.Count;
            ViewData["OrgDocCount"] = $"{documents.Count(d => d.IsUploaded)}/{documents.Count}";
            ViewData["OrgProfileIncomplete"] = okN < 4;

            return View(documents);
        }

        // The service enforces the configured size limit; this just lets the request reach it.
        [HttpPost]
        public async Task<IActionResult> Upload(Guid documentTypeId, IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                TempData["Error"] = "Choose a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            await using var content = file.OpenReadStream();
            var result = await customerDocumentService.UploadAsync(
                GetCustomerId(), documentTypeId, file.FileName, file.ContentType, file.Length, content);

            if (result.Succeeded)
            {
                TempData["Success"] = "Document uploaded.";
            }
            else
            {
                SetUploadErrorToast(result.Error);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid documentTypeId)
        {
            var result = await customerDocumentService.DownloadAsync(GetCustomerId(), documentTypeId);
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            // Served as a download and never sniffed, so an uploaded file cannot run as a web page.
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid documentTypeId)
        {
            var result = await customerDocumentService.RemoveAsync(GetCustomerId(), documentTypeId);
            if (result.Succeeded)
            {
                TempData["SuccessTitle"] = "Removed";
                TempData["Success"] = "File removed. You can upload another at any time.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Index));
        }

        private void SetUploadErrorToast(string? error)
        {
            TempData["Error"] = error;
            if (string.Equals(error, CustomerDocumentMessages.UnsupportedFileTypeBody, StringComparison.Ordinal))
            {
                TempData["ErrorTitle"] = CustomerDocumentMessages.UnsupportedFileTypeTitle;
            }
        }

        private Guid GetCustomerId()
            => Guid.Parse(User.FindFirst(AppClaimTypes.CustomerId)!.Value);
    }
}

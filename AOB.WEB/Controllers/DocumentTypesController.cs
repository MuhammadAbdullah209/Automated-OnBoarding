using AOB.BAL.DTOs.Documents;
using AOB.BAL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.Controllers
{
    /// <summary>Document types: super admins and internal users can view; only internal users can change them.</summary>
    [Authorize(Roles = AOB.BAL.Common.Constants.AppRoles.InternalAccess)]
    public class DocumentTypesController(IDocumentTypeService documentTypeService) : Controller
    {
        public async Task<IActionResult> Index()
            => View(await documentTypeService.GetAllAsync());

        [HttpGet]
        [Authorize(Roles = AOB.BAL.Common.Constants.AppRoles.InternalUser)]
        public IActionResult Create() => View(new DocumentTypeDto());

        [HttpPost]
        [Authorize(Roles = AOB.BAL.Common.Constants.AppRoles.InternalUser)]
        public async Task<IActionResult> Create(DocumentTypeDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await documentTypeService.CreateAsync(model);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(nameof(model.Name), result.Error ?? "Could not create the document type.");
                return View(model);
            }

            TempData["Success"] = $"Document type \"{model.Name}\" was created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = AOB.BAL.Common.Constants.AppRoles.InternalUser)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await documentTypeService.GetAsync(id);
            return result.Succeeded && result.Data is not null ? View(result.Data) : NotFound();
        }

        [HttpPost]
        [Authorize(Roles = AOB.BAL.Common.Constants.AppRoles.InternalUser)]
        public async Task<IActionResult> Edit(DocumentTypeDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await documentTypeService.UpdateAsync(model);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(nameof(model.Name), result.Error ?? "Could not update the document type.");
                return View(model);
            }

            TempData["Success"] = $"Document type \"{model.Name}\" was updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = AOB.BAL.Common.Constants.AppRoles.InternalUser)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await documentTypeService.DeleteAsync(id);

            if (result.Succeeded)
            {
                TempData["Success"] = "Document type deleted.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

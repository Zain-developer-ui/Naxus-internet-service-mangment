using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Procurement;

namespace NEXUS.Controllers
{
    /**
     * Supplier register. Admin-only - a retail clerk does not add vendors.
     */
    [Authorize(Roles = NexusRoles.Admin)]
    [Route("Admin/Vendors")]
    public class VendorsController : Controller
    {
        private readonly IVendorService _vendors;

        public VendorsController(IVendorService vendors) => _vendors = vendors;

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search, bool showInactive, CancellationToken ct)
        {
            ViewData.SetActiveItem("vendors");

            var result = await _vendors.ListAsync(search, showInactive, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new VendorListPage());
            }

            return View(result.Value!);
        }

        [HttpGet("New")]
        public IActionResult Create()
        {
            ViewData.SetActiveItem("vendors");
            return View("Edit", new VendorEditViewModel());
        }

        [HttpPost("New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("vendors");
            if (!ModelState.IsValid) return View("Edit", model);

            var result = await _vendors.SaveAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("Edit", model);
            }

            TempData["Success"] = $"Vendor '{model.Name}' added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/Edit")]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("vendors");

            var result = await _vendors.GetAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            return View(result.Value!);
        }

        [HttpPost("{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VendorEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("vendors");
            if (!ModelState.IsValid) return View(model);

            var result = await _vendors.SaveAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View(model);
            }

            TempData["Success"] = $"Vendor '{model.Name}' saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, bool active, CancellationToken ct)
        {
            var result = await _vendors.SetActiveAsync(id, active, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? (active ? "Vendor is active." : "Vendor suspended.")
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _vendors.DeleteAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Vendor removed."
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        private void ApplyErrors(Result<int> result)
        {
            if (result.Errors is null)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Could not save.");
                return;
            }

            foreach (var (field, messages) in result.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(field, message);
        }
    }
}

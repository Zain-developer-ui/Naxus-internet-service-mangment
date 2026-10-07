using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Catalog;

namespace NEXUS.Controllers
{
    /**
     * The catalogue console. Admin-only - a retail clerk orders from the
     * catalogue, only the administrator reshapes it.
     */
    [Authorize(Roles = NexusRoles.Admin)]
    [Route("Admin/Plans")]
    public class AdminPlansController : Controller
    {
        private readonly IPlanAdminService _plans;

        public AdminPlansController(IPlanAdminService plans) => _plans = plans;

        [HttpGet("")]
        public async Task<IActionResult> Index(ConnectionType? type, string? search,
                                               bool showInactive, CancellationToken ct)
        {
            ViewData.SetActiveItem("plans");

            var result = await _plans.ListAsync(type, search, showInactive, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new PlanListPage
                {
                    Filter = type,
                    Search = search,
                    ShowInactive = showInactive
                });
            }

            return View(result.Value!);
        }

        [HttpGet("New")]
        public IActionResult Create()
        {
            ViewData.SetActiveItem("plans");
            return View("Edit", PlanEditViewModel.Blank());
        }

        [HttpPost("New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlanEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("plans");
            NormalisePrices(model);

            if (!ModelState.IsValid) return View("Edit", model);

            var result = await _plans.CreateAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("Edit", model);
            }

            TempData["Success"] = $"Plan '{model.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/Edit")]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("plans");

            var result = await _plans.GetForEditAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            return View(result.Value!);
        }

        [HttpPost("{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PlanEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("plans");
            NormalisePrices(model);

            if (!ModelState.IsValid) return View(model);

            var result = await _plans.UpdateAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View(model);
            }

            TempData["Success"] = $"Plan '{model.Name}' saved.";
            return RedirectToAction(nameof(Index));
        }

        /**
         * Retire and restore. This is the safe alternative to deletion, and the
         * one the list steers an operator towards once a plan has orders.
         */
        [HttpPost("{id:int}/ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, bool active, ConnectionType? type,
                                                      string? search, bool showInactive,
                                                      CancellationToken ct)
        {
            var result = await _plans.SetActiveAsync(id, active, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? (active ? "Plan is now active." : "Plan retired - it is hidden from customers.")
                : result.Message;

            return RedirectToAction(nameof(Index), new { type, search, showInactive });
        }

        [HttpPost("{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _plans.DeleteAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Plan deleted."
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        /**
         * An unlimited plan has no hour bundle, and an unavailable cycle needs
         * no rate. Clearing both here keeps the validator from rejecting a form
         * the operator filled correctly.
         */
        private static void NormalisePrices(PlanEditViewModel model)
        {
            if (model.IsUnlimited) model.HoursIncluded = null;

            foreach (var row in model.Prices.Where(r => !r.IsAvailable))
                row.Amount = 0m;
        }

        private void ApplyErrors(Result<int> result)
        {
            if (result.Errors is null)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Could not save the plan.");
                return;
            }

            foreach (var (field, messages) in result.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(field, message);
        }
    }
}

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
     * Purchase orders. Admin-only, like the vendor register it hangs off.
     */
    [Authorize(Roles = NexusRoles.Admin)]
    [Route("Admin/PurchaseOrders")]
    public class PurchaseOrdersController : Controller
    {
        private readonly IPurchaseOrderService _orders;

        public PurchaseOrdersController(IPurchaseOrderService orders) => _orders = orders;

        [HttpGet("")]
        public async Task<IActionResult> Index(string? status, int? vendorId, CancellationToken ct)
        {
            ViewData.SetActiveItem("purchase-orders");

            var result = await _orders.ListAsync(status, vendorId, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new PurchaseOrderListPage());
            }

            return View(result.Value!);
        }

        [HttpGet("New")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            ViewData.SetActiveItem("purchase-orders");

            var model = new PurchaseOrderEditViewModel { Status = PurchaseOrderService.Ordered };
            await FillAsync(model, ct);

            return View("Edit", model);
        }

        [HttpPost("New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("purchase-orders");
            if (!ModelState.IsValid)
            {
                await FillAsync(model, ct);
                return View("Edit", model);
            }

            var result = await _orders.CreateAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                await FillAsync(model, ct);
                return View("Edit", model);
            }

            TempData["Success"] = "Purchase order raised.";
            return RedirectToAction(nameof(Details), new { id = result.Value });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("purchase-orders");

            var result = await _orders.GetDetailAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            return View(result.Value!);
        }

        [HttpPost("{id:int}/Receive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int id, int shopId, CancellationToken ct)
        {
            var result = await _orders.ReceiveAsync(id, shopId, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Goods received. Stock updated."
                : result.Message;

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("{id:int}/Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, CancellationToken ct)
        {
            var result = await _orders.CancelAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Purchase order cancelled."
                : result.Message;

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task FillAsync(PurchaseOrderEditViewModel model, CancellationToken ct)
        {
            var options = await _orders.GetOptionsAsync(ct);
            if (options.IsSuccess)
            {
                model.Vendors = options.Value!.Vendors;
                model.Products = options.Value!.Products;
            }
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

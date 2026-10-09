using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Inventory;

namespace NEXUS.Controllers
{
    /**
     * Equipment stock and the product catalogue behind it. Admin and Retail
     * both work stock - the warehouse and the counter are the same ledger.
     */
    [Authorize(Roles = NexusRoles.Admin + "," + NexusRoles.Retail)]
    [Route("Admin/Inventory")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventory;

        public InventoryController(IInventoryService inventory) => _inventory = inventory;

        [HttpGet("")]
        public async Task<IActionResult> Index(int? shopId, string? search, bool lowOnly, CancellationToken ct)
        {
            ViewData.SetActiveItem("inventory");

            var result = await _inventory.ListStockAsync(shopId, search, lowOnly, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new StockListPage());
            }

            return View(result.Value!);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("inventory");

            var result = await _inventory.GetStockAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            return View(result.Value!);
        }

        [HttpPost("{id:int}/Adjust")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(StockAdjustViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("inventory");

            var result = await _inventory.AdjustAsync(model, ct);

            if (!result.IsSuccess)
            {
                // The view binds the form through the Adjust.* prefix, so the
                // service's bare property names have to be namespaced to land
                // next to the input that caused them.
                if (result.Errors is null)
                {
                    ModelState.AddModelError(string.Empty, result.Message ?? "Could not adjust stock.");
                }
                else
                {
                    foreach (var (field, messages) in result.Errors)
                        foreach (var message in messages)
                            ModelState.AddModelError($"Adjust.{field}", message);
                }

                var page = await _inventory.GetStockAsync(model.StockItemId, ct);
                if (!page.IsSuccess) return NotFound();

                page.Value!.Adjust.QuantityDelta = model.QuantityDelta;
                page.Value!.Adjust.Reason = model.Reason;
                return View("Detail", page.Value!);
            }

            TempData["Success"] = $"Stock adjusted. On hand is now {result.Value}.";
            return RedirectToAction(nameof(Detail), new { id = model.StockItemId });
        }

        // --------------------------------------------------------- products

        [HttpGet("Products")]
        public async Task<IActionResult> Products(string? search, bool showInactive, CancellationToken ct)
        {
            ViewData.SetActiveItem("inventory-products");

            var result = await _inventory.ListProductsAsync(search, showInactive, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new ProductListPage());
            }

            return View(result.Value!);
        }

        [HttpGet("Products/New")]
        public IActionResult CreateProduct()
        {
            ViewData.SetActiveItem("inventory-products");
            return View("ProductEdit", new ProductEditViewModel());
        }

        [HttpPost("Products/New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("inventory-products");
            if (!ModelState.IsValid) return View("ProductEdit", model);

            var result = await _inventory.SaveProductAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("ProductEdit", model);
            }

            TempData["Success"] = $"'{model.Name}' added to the equipment catalogue.";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet("Products/{id:int}/Edit")]
        public async Task<IActionResult> EditProduct(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("inventory-products");

            var result = await _inventory.GetProductAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            return View("ProductEdit", result.Value!);
        }

        [HttpPost("Products/{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("inventory-products");
            if (!ModelState.IsValid) return View("ProductEdit", model);

            var result = await _inventory.SaveProductAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("ProductEdit", model);
            }

            TempData["Success"] = $"'{model.Name}' saved.";
            return RedirectToAction(nameof(Products));
        }

        [HttpPost("Products/{id:int}/ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProduct(int id, bool active, CancellationToken ct)
        {
            var result = await _inventory.SetProductActiveAsync(id, active, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? (active ? "Equipment is active." : "Equipment retired.")
                : result.Message;

            return RedirectToAction(nameof(Products));
        }

        [HttpPost("Products/{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
        {
            var result = await _inventory.DeleteProductAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Equipment removed."
                : result.Message;

            return RedirectToAction(nameof(Products));
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

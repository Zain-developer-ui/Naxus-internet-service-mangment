using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Settings;

namespace NEXUS.Controllers
{
    /**
     * The operating footprint console: which cities the ISP covers, and the
     * bulk discount slabs applied to large orders. Admin-only.
     */
    [Authorize(Roles = NexusRoles.Admin)]
    [Route("Admin/Settings")]
    public class AdminSettingsController : Controller
    {
        private readonly ISettingsAdminService _settings;

        public AdminSettingsController(ISettingsAdminService settings) => _settings = settings;

        // ------------------------------------------------------------ cities

        [HttpGet("Cities")]
        public async Task<IActionResult> Cities(string? search, CancellationToken ct)
        {
            ViewData.SetActiveItem("settings-cities");

            var result = await _settings.ListCitiesAsync(search, ct);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new CityListPage { Search = search });
            }

            return View(result.Value!);
        }

        [HttpGet("Cities/New")]
        public IActionResult CreateCity()
        {
            ViewData.SetActiveItem("settings-cities");
            return View("CityEdit", new CityEditViewModel { Code = 100 });
        }

        [HttpPost("Cities/New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCity(CityEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("settings-cities");
            if (!ModelState.IsValid) return View("CityEdit", model);

            var result = await _settings.SaveCityAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("CityEdit", model);
            }

            TempData["Success"] = $"City '{model.Name}' added.";
            return RedirectToAction(nameof(Cities));
        }

        [HttpGet("Cities/{id:int}/Edit")]
        public async Task<IActionResult> EditCity(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("settings-cities");

            var result = await _settings.GetCityAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            return View("CityEdit", result.Value!);
        }

        [HttpPost("Cities/{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCity(CityEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("settings-cities");
            if (!ModelState.IsValid) return View("CityEdit", model);

            var result = await _settings.SaveCityAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("CityEdit", model);
            }

            TempData["Success"] = $"City '{model.Name}' saved.";
            return RedirectToAction(nameof(Cities));
        }

        [HttpPost("Cities/{id:int}/ToggleServed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCityServed(int id, bool served, CancellationToken ct)
        {
            var result = await _settings.ToggleCityServedAsync(id, served, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? (served ? "City is now offered to new customers." : "City hidden from new registrations.")
                : result.Message;

            return RedirectToAction(nameof(Cities));
        }

        [HttpPost("Cities/{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCity(int id, CancellationToken ct)
        {
            var result = await _settings.DeleteCityAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "City removed."
                : result.Message;

            return RedirectToAction(nameof(Cities));
        }

        // ---------------------------------------------------- discount tiers

        [HttpGet("Discounts")]
        public async Task<IActionResult> Discounts(CancellationToken ct)
        {
            ViewData.SetActiveItem("settings-discounts");

            var result = await _settings.ListTiersAsync(ct);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new DiscountTierListPage());
            }

            return View(result.Value!);
        }

        [HttpGet("Discounts/New")]
        public IActionResult CreateTier()
        {
            ViewData.SetActiveItem("settings-discounts");
            return View("TierEdit", new DiscountTierEditViewModel { MinConnections = 1, Rate = 0.05m });
        }

        [HttpPost("Discounts/New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTier(DiscountTierEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("settings-discounts");
            if (!ModelState.IsValid) return View("TierEdit", model);

            var result = await _settings.SaveTierAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("TierEdit", model);
            }

            TempData["Success"] = $"Discount slab {model.Label} added.";
            return RedirectToAction(nameof(Discounts));
        }

        [HttpGet("Discounts/{id:int}/Edit")]
        public async Task<IActionResult> EditTier(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("settings-discounts");

            var result = await _settings.GetTierAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            return View("TierEdit", result.Value!);
        }

        [HttpPost("Discounts/{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTier(DiscountTierEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("settings-discounts");
            if (!ModelState.IsValid) return View("TierEdit", model);

            var result = await _settings.SaveTierAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("TierEdit", model);
            }

            TempData["Success"] = "Discount slab saved.";
            return RedirectToAction(nameof(Discounts));
        }

        [HttpPost("Discounts/{id:int}/ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTier(int id, bool active, CancellationToken ct)
        {
            var result = await _settings.ToggleTierAsync(id, active, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? (active ? "Slab is active." : "Slab deactivated - it will not be applied to new bills.")
                : result.Message;

            return RedirectToAction(nameof(Discounts));
        }

        [HttpPost("Discounts/{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTier(int id, CancellationToken ct)
        {
            var result = await _settings.DeleteTierAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Slab removed."
                : result.Message;

            return RedirectToAction(nameof(Discounts));
        }

        /**
         * The service reports field level problems against the model property
         * names, so they land next to the input that caused them.
         */
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

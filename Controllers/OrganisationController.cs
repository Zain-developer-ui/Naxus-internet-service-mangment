using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Organisation;

namespace NEXUS.Controllers
{
    /**
     * Outlets and staff records. Admin-only: an outlet code appears on receipts
     * and employee codes on job sheets, so both are controlled registers.
     */
    [Authorize(Roles = NexusRoles.Admin)]
    [Route("Admin/Organisation")]
    public class OrganisationController : Controller
    {
        private readonly IOrganisationService _org;

        public OrganisationController(IOrganisationService org) => _org = org;

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");

            var result = await _org.ListAsync(ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new OrgListPage());
            }

            return View(result.Value!);
        }

        // ---------------------------------------------------------------- shops

        [HttpGet("Shops/New")]
        public async Task<IActionResult> CreateShop(CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");
            var cities = await _org.CityOptionsAsync(ct);
            return View("ShopEdit", OrganisationService.BlankShop(cities));
        }

        [HttpPost("Shops/New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShop(ShopEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");
            if (!ModelState.IsValid) return View("ShopEdit", await RefillAsync(model, ct));

            var result = await _org.SaveShopAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("ShopEdit", await RefillAsync(model, ct));
            }

            TempData["Success"] = $"Outlet '{model.Name}' added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Shops/{id:int}/Edit")]
        public async Task<IActionResult> EditShop(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");

            var result = await _org.GetShopAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            return View("ShopEdit", result.Value!);
        }

        [HttpPost("Shops/{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShop(ShopEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");
            if (!ModelState.IsValid) return View("ShopEdit", await RefillAsync(model, ct));

            var result = await _org.SaveShopAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("ShopEdit", await RefillAsync(model, ct));
            }

            TempData["Success"] = $"Outlet '{model.Name}' saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Shops/{id:int}/ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleShop(int id, bool active, CancellationToken ct)
        {
            var result = await _org.SetShopActiveAsync(id, active, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? (active ? "Outlet reopened." : "Outlet closed for new business.")
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Shops/{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShop(int id, CancellationToken ct)
        {
            var result = await _org.DeleteShopAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Outlet removed."
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------ employees

        [HttpGet("Employees/New")]
        public async Task<IActionResult> CreateEmployee(CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");

            var lists = await _org.GetListsAsync(ct);
            if (!lists.IsSuccess) return NotFound();

            return View("EmployeeEdit", new EmployeeEditViewModel
            {
                Shops = lists.Value!.Shops,
                Users = lists.Value!.Users.Where(u => !u.AlreadyLinked).ToList()
            });
        }

        [HttpPost("Employees/New")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(EmployeeEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");
            if (!ModelState.IsValid) return View("EmployeeEdit", await RefillEmployeeAsync(model, ct));

            var result = await _org.SaveEmployeeAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("EmployeeEdit", await RefillEmployeeAsync(model, ct));
            }

            TempData["Success"] = $"'{model.FullName}' added to the staff register.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Employees/{id:int}/Edit")]
        public async Task<IActionResult> EditEmployee(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");

            var lists = await _org.GetListsAsync(ct);
            if (!lists.IsSuccess) return NotFound();

            // Employee read model is intentionally thin; the edit form re-reads
            // the record through the lists call plus the row it is editing.
            var row = lists.Value!.Shops;
            var model = await BuildEmployeeAsync(id, ct);
            if (model is null) return NotFound();

            model.Shops = row;
            model.Users = lists.Value!.Users;
            return View("EmployeeEdit", model);
        }

        [HttpPost("Employees/{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(EmployeeEditViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("organisation");
            if (!ModelState.IsValid) return View("EmployeeEdit", await RefillEmployeeAsync(model, ct));

            var result = await _org.SaveEmployeeAsync(model, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View("EmployeeEdit", await RefillEmployeeAsync(model, ct));
            }

            TempData["Success"] = $"'{model.FullName}' saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Employees/{id:int}/ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEmployee(int id, bool active, CancellationToken ct)
        {
            var result = await _org.SetEmployeeActiveAsync(id, active, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? (active ? "Employee is active." : "Employee marked inactive.")
                : result.Message;

            return RedirectToAction(nameof(Index));
        }

        private async Task<ShopEditViewModel> RefillAsync(ShopEditViewModel model, CancellationToken ct)
        {
            model.Cities = await _org.CityOptionsAsync(ct);
            return model;
        }

        private async Task<EmployeeEditViewModel> RefillEmployeeAsync(EmployeeEditViewModel model, CancellationToken ct)
        {
            var lists = await _org.GetListsAsync(ct);
            if (lists.IsSuccess)
            {
                model.Shops = lists.Value!.Shops;
                model.Users = lists.Value!.Users;
            }
            return model;
        }

        /** The edit form needs the raw employee fields plus the select options. */
        private async Task<EmployeeEditViewModel?> BuildEmployeeAsync(int id, CancellationToken ct)
        {
            var result = await _org.GetEmployeeAsync(id, ct);
            if (!result.IsSuccess) return null;

            var e = result.Value!;
            return new EmployeeEditViewModel
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                Designation = e.Designation,
                Phone = e.Phone,
                Email = string.IsNullOrWhiteSpace(e.Email) ? null : e.Email,
                IsActive = e.IsActive,
                ShopId = e.ShopId,
                UserId = e.UserId,
                JoinedOn = e.JoinedOnDate == default ? DateTime.UtcNow.Date : e.JoinedOnDate
            };
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

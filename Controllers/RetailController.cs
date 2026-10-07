using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Dashboards;
using NEXUS.Services.Registration;
using NEXUS.Services.Search;

namespace NEXUS.Controllers
{
    [Authorize(Roles = NexusRoles.Retail + "," + NexusRoles.Admin)]
    public class RetailController : Controller
    {
        private readonly IRetailDashboardService _dash;
        private readonly ISearchService _search;
        private readonly IRegistrationService _registration;
        private readonly ILogger<RetailController> _logger;

        public RetailController(IRetailDashboardService dash, ISearchService search,
                                IRegistrationService registration,
                                ILogger<RetailController> logger)
        {
            _dash = dash;
            _search = search;
            _registration = registration;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            ViewData.SetActiveItem("dashboard");
            var result = await _dash.GetAsync(ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new RetailDashboardData());
            }

            return View(result.Value!);
        }

        /**
         * Walk-in registration. Same service the public signup uses, except the
         * clerk is signed in already and the shop is attached to the record.
         */
        [HttpGet]
        public async Task<IActionResult> NewOrder(CancellationToken ct)
        {
            ViewData.SetActiveItem("neworder");
            var options = await _registration.GetOptionsAsync(ct);

            if (!options.IsSuccess)
            {
                TempData["Error"] = options.Message;
                return View(new OrderViewModel());
            }

            return View(ApplyOptions(new OrderViewModel(), options.Value!));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewOrder(OrderViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("neworder");
            var options = await _registration.GetOptionsAsync(ct);

            if (!options.IsSuccess)
            {
                TempData["Error"] = options.Message;
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                ApplyOptions(model, options.Value!);
                return View(model);
            }

            var result = await _registration.RegisterAsync(model, shopId: null, ct);

            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                ApplyOptions(model, options.Value!);
                return View(model);
            }

            var customer = result.Value!;

            _logger.LogInformation(
                "Retail order {OrderId} raised for {AccountId} by {Clerk}",
                customer.OrderId, customer.AccountId, User.DisplayName());

            TempData["Success"] = $"Order {customer.OrderId} raised. " +
                                  $"Account ID {customer.AccountId} - hand these to the customer.";

            return RedirectToAction(nameof(Search), new { q = customer.AccountId });
        }

        public async Task<IActionResult> Search(string? q, CancellationToken ct)
        {
            ViewData.SetActiveItem("search");
            var result = await _search.SearchAsync(q, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new SearchPageData { Term = q });
            }

            return View(result.Value!);
        }

        private void ApplyErrors(Result<RegisteredCustomer> result)
        {
            if (result.Errors is null)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Could not place the order.");
                return;
            }

            foreach (var (field, messages) in result.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(field, message);
        }

        private static OrderViewModel ApplyOptions(OrderViewModel model, RegistrationOptions options)
        {
            model.Cities = options.Cities;
            model.Plans = options.Plans;
            return model;
        }
    }
}

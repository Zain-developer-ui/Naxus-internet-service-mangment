using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.CustomerPortal;
using NEXUS.Services.Orders;

namespace NEXUS.Controllers
{
    /**
     * The customer's own portal. Customers see their own record; staff may open
     * a specific customer by ID, which is what the admin "view as" links need.
     * A customer can never widen the scope - the ID is taken from the claims
     * cookie and any id they supply is ignored.
     */
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerPortalService _portal;
        private readonly IPlanChangeService _planChanges;

        public CustomerController(ICustomerPortalService portal,
                                  IPlanChangeService planChanges)
        {
            _portal = portal;
            _planChanges = planChanges;
        }

        public async Task<IActionResult> Dashboard(int? id, CancellationToken ct)
        {
            var customerId = ResolveCustomerId(id);
            if (customerId is null) return RedirectToAction("Login", "Account");

            var result = await _portal.GetDashboardAsync(customerId.Value, ct);
            if (!result.IsSuccess) return NotFound();

            ViewData.SetActiveItem("dashboard");
            return View(result.Value);
        }

        public async Task<IActionResult> MyConnection(int? id, CancellationToken ct)
        {
            var customerId = ResolveCustomerId(id);
            if (customerId is null) return RedirectToAction("Login", "Account");

            var result = await _portal.GetAccountAsync(customerId.Value, ct);
            if (!result.IsSuccess) return NotFound();

            ViewData.SetActiveItem("myconnection");
            return View(result.Value);
        }

        /**
         * Plan change. A customer asked to be moved to a different plan is a
         * request a clerk approves - never a switch the customer flips, since
         * the new deposit and speed have to be confirmed against the line.
         */
        [HttpGet]
        public async Task<IActionResult> Upgrade(CancellationToken ct)
        {
            var customerId = User.CustomerId();
            if (customerId is null) return RedirectToAction("Login", "Account");

            ViewData.SetActiveItem("myconnection");

            var result = await _planChanges.GetForCustomerAsync(customerId.Value, ct);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(MyConnection));
            }

            return View(result.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upgrade(PlanChangeInput input, CancellationToken ct)
        {
            var customerId = User.CustomerId();
            if (customerId is null) return RedirectToAction("Login", "Account");

            ViewData.SetActiveItem("myconnection");

            var result = await _planChanges.SubmitAsync(input, customerId.Value, ct);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Upgrade));
            }

            TempData["Success"] = "Your plan change request has been sent for approval.";
            return RedirectToAction(nameof(MyConnection));
        }

        /**
         * A customer is pinned to their own ID. Staff may pass ?id= to inspect
         * a particular account; without it they are sent to their own dashboard
         * rather than being shown an arbitrary record.
         */
        private int? ResolveCustomerId(int? requested)
        {
            var own = User.CustomerId();
            if (own is not null) return own;

            return User.IsStaff() ? requested : null;
        }
    }
}

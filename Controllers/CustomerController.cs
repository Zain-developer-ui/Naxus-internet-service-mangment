using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Services.CustomerPortal;

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

        public CustomerController(ICustomerPortalService portal) => _portal = portal;

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

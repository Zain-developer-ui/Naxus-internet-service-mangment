using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Authentication;
using NEXUS.Services.Orders;
using NEXUS.Services.Registration;

namespace NEXUS.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderTrackingService _tracking;
        private readonly IRegistrationService _registration;
        private readonly IUserAuthenticator _auth;
        private readonly ISignInService _signIn;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderTrackingService tracking, IRegistrationService registration,
                                IUserAuthenticator auth, ISignInService signIn,
                                ILogger<OrdersController> logger)
        {
            _tracking = tracking;
            _registration = registration;
            _auth = auth;
            _signIn = signIn;
            _logger = logger;
        }

        /**
         * Public application form. Someone who is already signed in has no
         * business creating a second customer account, so they are sent to
         * their dashboard instead.
         */
        [HttpGet]
        public async Task<IActionResult> New(CancellationToken ct)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Customer");

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
        public async Task<IActionResult> New(OrderViewModel model, CancellationToken ct)
        {
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
                "Order {OrderId} raised for new customer {AccountId} ({ConnectionType})",
                customer.OrderId, customer.AccountId, model.ConnectionType);

            // The customer just chose a password, so signing them straight in
            // saves a second trip through the login page.
            var authenticated = await _auth.ValidateCredentialsAsync(
                customer.AccountId, model.Password, ct);

            if (authenticated.IsSuccess)
            {
                await _signIn.SignInAsync(HttpContext, authenticated.Value!, persistent: false);
                await _auth.RecordSuccessfulLoginAsync(authenticated.Value!.User.Id, ct);
            }

            TempData["Success"] = $"Order {customer.OrderId} received. " +
                                  $"Your account ID is {customer.AccountId}.";

            return RedirectToAction("Dashboard", "Customer");
        }

        /**
         * Track by reference. A customer sees only their own orders - retail,
         * technical and admin are staff and may look up any reference.
         */
        [Authorize(Roles = NexusRoles.Customer + "," + NexusRoles.Retail + "," +
                           NexusRoles.Technical + "," + NexusRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> Tracking(string? id, CancellationToken ct)
        {
            ViewData.SetActiveItem("orders");

            var scopedId = User.IsInRole(NexusRoles.Customer) ? User.CustomerId() : null;
            var reference = string.IsNullOrWhiteSpace(id) ? null : id.Trim();

            if (reference is null)
            {
                if (scopedId is not int customerId) return View(null);

                var history = await _tracking.ForCustomerAsync(customerId, ct);
                ViewBag.History = history.IsSuccess
                    ? history.Value!
                    : Array.Empty<OrderTrackingSummary>();

                return View(null);
            }

            var result = await _tracking.GetAsync(reference, scopedId, ct);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                ViewBag.OrderId = reference;
                return View(null);
            }

            ViewBag.OrderId = result.Value!.OrderId;
            return View(result.Value);
        }

        /**
         * Field errors are keyed by property name so they land on the same span
         * the client-side rules use. Anything without a matching field falls
         * back to the summary.
         */
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

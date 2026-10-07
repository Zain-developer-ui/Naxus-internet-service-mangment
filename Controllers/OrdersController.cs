using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Api;
using NEXUS.Services.Authentication;
using NEXUS.Services.Registration;

namespace NEXUS.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IApiService _api;
        private readonly IRegistrationService _registration;
        private readonly IUserAuthenticator _auth;
        private readonly ISignInService _signIn;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IApiService api, IRegistrationService registration,
                                IUserAuthenticator auth, ISignInService signIn,
                                ILogger<OrdersController> logger)
        {
            _api = api;
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

        [Authorize(Roles = NexusRoles.Customer + "," + NexusRoles.Retail + "," +
                           NexusRoles.Technical + "," + NexusRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> Tracking(string? id)
        {
            ViewBag.OrderId = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
            var order = ViewBag.OrderId is null ? null : await _api.GetOrderAsync(ViewBag.OrderId);
            return View(order);
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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Api;
using NEXUS.Services.Authentication;

namespace NEXUS.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiService _api;
        private readonly IUserAuthenticator _auth;
        private readonly ISignInService _signIn;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IApiService api, IUserAuthenticator auth,
                                 ISignInService signIn, ILogger<AccountController> logger)
        {
            _api = api;
            _auth = auth;
            _signIn = signIn;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl,
                                               CancellationToken ct)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid) return View(model);

            var result = await _auth.ValidateCredentialsAsync(
                model.AccountIdOrEmail, model.Password, ct);

            if (!result.IsSuccess)
            {
                // The service deliberately returns one message for every failure;
                // surfacing it verbatim keeps the response uniform.
                ModelState.AddModelError(string.Empty, result.Message ?? "Sign in failed.");
                return View(model);
            }

            var authenticated = result.Value!;

            // A fresh cookie on every successful sign in, so a token captured
            // before login cannot be reused afterwards.
            await _signIn.SignOutAsync(HttpContext);
            await _signIn.SignInAsync(HttpContext, authenticated, model.RememberMe);
            await _auth.RecordSuccessfulLoginAsync(authenticated.User.Id, ct);

            _logger.LogInformation("User {AccountId} signed in as {Role}",
                authenticated.User.AccountId, authenticated.Role);

            if (authenticated.User.MustChangePassword)
                return RedirectToAction(nameof(ChangePassword));

            return RedirectToLocal(returnUrl) ?? RedirectToRoleHome(authenticated.Role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var account = User.AccountId();
            await _signIn.SignOutAsync(HttpContext);
            _logger.LogInformation("User {AccountId} signed out", account);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Wired up with the rest of the account lifecycle; the seeded admin
            // is the only account that reaches this screen today.
            ModelState.AddModelError(string.Empty,
                "Password change is not enabled yet.");
            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        /**
         * Sign up and order a connection are the same act - the application
         * form creates the account as part of raising the order. The old
         * standalone registration page is gone, so anything still linking to
         * it lands on the application form instead of a second, parallel flow.
         */
        [HttpGet]
        public IActionResult Register() => RedirectToAction("New", "Orders");

        [HttpGet]
        public IActionResult Status() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Status(string? accountId, string? phone, string? cnic)
        {
            AccountViewModel? acc = null;

            if (!string.IsNullOrWhiteSpace(accountId))
                acc = await _api.GetCustomerAsync(accountId);
            else if (!string.IsNullOrWhiteSpace(phone))
                acc = await _api.GetCustomerByPhoneAsync(phone);
            else if (!string.IsNullOrWhiteSpace(cnic))
                acc = await _api.GetCustomerByCnicAsync(cnic);

            ViewBag.Searched = true;
            return View(acc);
        }

        /**
         * Only a path that stays inside this site is honoured. An absolute URL
         * supplied as returnUrl would otherwise turn the login page into an
         * open redirect.
         */
        private IActionResult? RedirectToLocal(string? returnUrl) =>
            !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : null;

        private IActionResult RedirectToRoleHome(string role) => role switch
        {
            NexusRoles.Admin => RedirectToAction("Dashboard", "Admin"),
            NexusRoles.Accounts => RedirectToAction("Dashboard", "Accounts"),
            NexusRoles.Technical => RedirectToAction("Dashboard", "Technical"),
            NexusRoles.Retail => RedirectToAction("Dashboard", "Retail"),
            _ => RedirectToAction("Dashboard", "Customer")
        };
    }
}

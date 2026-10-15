using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Authentication;
using NEXUS.Services.CustomerPortal;
using NEXUS.Services.Email;

namespace NEXUS.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserAuthenticator _auth;
        private readonly ISignInService _signIn;
        private readonly ICustomerPortalService _portal;
        private readonly IPasswordResetService _resets;
        private readonly IEmailSender _emails;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserAuthenticator auth, ISignInService signIn,
                                 ICustomerPortalService portal, IPasswordResetService resets,
                                 IEmailSender emails,
                                 ILogger<AccountController> logger)
        {
            _auth = auth;
            _signIn = signIn;
            _portal = portal;
            _resets = resets;
            _emails = emails;
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
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl,
                                               CancellationToken ct)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // The limiter redirects back here once a caller is throttled; the
            // flag turns that into a visible message instead of a silent reload.
            if (Request.Query.ContainsKey("throttled"))
                ModelState.AddModelError(string.Empty,
                    "Too many sign in attempts. Please wait a minute and try again.");

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
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.UserId();
            if (userId is not int id)
                return RedirectToAction(nameof(Login));

            var result = await _auth.ChangePasswordAsync(
                id, model.CurrentPassword, model.NewPassword, ct);

            if (!result.IsSuccess)
            {
                if (result.Errors is not null)
                {
                    foreach (var (field, messages) in result.Errors)
                        foreach (var message in messages)
                            ModelState.AddModelError(field, message);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.Message ?? "Could not change the password.");
                }

                return View(model);
            }

            _logger.LogInformation("User {AccountId} changed their password", User.AccountId());
            TempData["Success"] = "Your password has been changed.";

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ForgotPassword(CancellationToken ct)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            var mail = await _emails.CurrentAsync(ct);
            return View(new ForgotPasswordViewModel { EmailEnabled = mail.IsUsable });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model,
                                                        CancellationToken ct)
        {
            var mail = await _emails.CurrentAsync(ct);
            model.EmailEnabled = mail.IsUsable;

            if (Request.Query.ContainsKey("throttled"))
                ModelState.AddModelError(string.Empty,
                    "Too many attempts. Please wait a minute and try again.");

            if (!ModelState.IsValid) return View(model);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _resets.BeginAsync(model.AccountIdOrEmail, model.Phone, baseUrl, ct);

            if (!result.IsSuccess)
            {
                if (result.Errors is not null)
                {
                    foreach (var (field, messages) in result.Errors)
                        foreach (var message in messages)
                            ModelState.AddModelError(field, message);
                }
                else
                {
                    ModelState.AddModelError(string.Empty,
                        result.Message ?? "Could not start the reset.");
                }

                return View(model);
            }

            var start = result.Value!;

            // Nothing is confirmed either way when the link goes out by mail, so
            // the wording cannot be used to test whether an account exists.
            if (start.Channel == "email")
            {
                TempData["Success"] =
                    "If those details match an account, a reset link is on its way to the address on file.";

                return RedirectToAction(nameof(Login));
            }

            _logger.LogInformation("Password reset started for {Key}", model.AccountIdOrEmail);
            return RedirectToAction(nameof(ResetPassword), new { token = start.Token });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token)
        {
            // Landing here without a token means a stale bookmark or a hand typed
            // URL, so send them back to the start rather than showing a dead form.
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(ForgotPassword));

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model,
                                                       CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _resets.CompleteAsync(model.Token, model.NewPassword, ct);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Could not reset the password.");
                return View(model);
            }

            _logger.LogInformation("Password reset completed for user {UserId}", result.Value);
            TempData["Success"] = "Your password has been reset. Sign in with the new one.";

            return RedirectToAction(nameof(Login));
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
        public async Task<IActionResult> Status(CancellationToken ct)
        {
            ViewData.SetActiveItem("status");

            // A signed-in customer checking their own status has nothing to
            // type - their account id is already known.
            if (User.IsInRole(NexusRoles.Customer) && User.AccountId() is string own)
            {
                var mine = await _portal.LookupAsync(own, null, null, ct);
                if (mine.IsSuccess)
                {
                    ViewBag.Searched = true;
                    return View(mine.Value);
                }
            }

            return View();
        }

        /**
         * Public status lookup. Deliberately unauthenticated - the SRS treats
         * this as a convenience check - so the result is a narrow summary and
         * never the customer's full record.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Status(string? accountId, string? phone, string? cnic,
                                                CancellationToken ct)
        {
            ViewData.SetActiveItem("status");
            ViewBag.Searched = true;

            var entry = new[] { accountId, phone, cnic }
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            if (entry is null)
            {
                ViewBag.Error = "Enter an Account ID, mobile number or CNIC.";
                return View(null);
            }

            var lookup = await _portal.LookupAsync(accountId, phone, cnic, ct);

            if (!lookup.IsSuccess)
            {
                ViewBag.Error = lookup.Message;
                return View(null);
            }

            return View(lookup.Value);
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

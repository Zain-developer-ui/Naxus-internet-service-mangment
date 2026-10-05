using Microsoft.AspNetCore.Mvc;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Api;

namespace NEXUS.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiService _api;
        public AccountController(IApiService api) => _api = api;

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // FRONTEND ONLY: any valid form redirects to the demo dashboard.
            // Backend developer replaces this with real authentication.
            TempData["Success"] = "Logged in successfully (demo).";
            return RedirectToAction("Dashboard", "Customer");
        }

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
    }
}
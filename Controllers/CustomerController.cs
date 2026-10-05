using Microsoft.AspNetCore.Mvc;
using NEXUS.Services.Api;

namespace NEXUS.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IApiService _api;

        public CustomerController(IApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Dashboard()
        {
            var vm = await _api.GetCustomerDashboardAsync("NX12345678");
            return View(vm);
        }

        public async Task<IActionResult> MyConnection()
        {
            var acc = await _api.GetCustomerAsync("NX12345678");
            return View(acc);
        }
    }
}
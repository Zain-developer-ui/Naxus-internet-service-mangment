using Microsoft.AspNetCore.Mvc;
using NEXUS.Services.Api;

namespace NEXUS.Controllers
{
    public class BillsController : Controller
    {
        private readonly IApiService _api;
        public BillsController(IApiService api) => _api = api;

        public async Task<IActionResult> Index()
        {
            var bills = await _api.GetBillsAsync("NX12345678");
            return View(bills);
        }

        public async Task<IActionResult> Details(string id = "INV-1003")
        {
            var bill = await _api.GetBillDetailsAsync(id);
            if (bill == null) return NotFound();
            return View(bill);
        }
    }
}
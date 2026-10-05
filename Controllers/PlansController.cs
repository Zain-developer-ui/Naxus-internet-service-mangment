using Microsoft.AspNetCore.Mvc;
using NEXUS.Services.Api;

namespace NEXUS.Controllers
{
    public class PlansController : Controller
    {
        private readonly IApiService _api;
        public PlansController(IApiService api) => _api = api;

        public async Task<IActionResult> Index(string? type)
        {
            var plans = await _api.GetPlansAsync(type);
            ViewBag.ServiceType = string.IsNullOrWhiteSpace(type) ? "Broadband" : type;
            return View(plans);
        }

        public async Task<IActionResult> Details(int id = 2)
        {
            var plan = await _api.GetPlanAsync(id);
            if (plan == null) return NotFound();
            return View(plan);
        }
    }
}
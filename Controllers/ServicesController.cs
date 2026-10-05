using Microsoft.AspNetCore.Mvc;
using NEXUS.Services.Api;

namespace NEXUS.Controllers
{
    public class ServicesController : Controller
    {
        private readonly IApiService _api;
        public ServicesController(IApiService api) => _api = api;

        public async Task<IActionResult> Index()
        {
            var services = await _api.GetServicesAsync();
            return View(services);  // ServiceViewModel list
        }
    }
}
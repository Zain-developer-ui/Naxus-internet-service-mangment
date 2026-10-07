using Microsoft.AspNetCore.Mvc;
using NEXUS.Services.Catalog;

namespace NEXUS.Controllers
{
    public class ServicesController : Controller
    {
        private readonly IServiceCatalogService _services;

        public ServicesController(IServiceCatalogService services) => _services = services;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var result = await _services.GetAllAsync(ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(Array.Empty<NEXUS.Models.ViewModels.ServiceViewModel>());
            }

            return View(result.Value!);
        }

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var result = await _services.GetAsync(id, ct);
            if (!result.IsSuccess) return NotFound();
            return View(result.Value);
        }
    }
}

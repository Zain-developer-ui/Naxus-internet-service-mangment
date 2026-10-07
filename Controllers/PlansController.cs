using Microsoft.AspNetCore.Mvc;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Catalog;

namespace NEXUS.Controllers
{
    public class PlansController : Controller
    {
        private readonly IPlanCatalogService _catalog;

        public PlansController(IPlanCatalogService catalog) => _catalog = catalog;

        public async Task<IActionResult> Index(string? type, CancellationToken ct)
        {
            var result = await _catalog.GetCatalogueAsync(type, ct);

            return result.IsSuccess
                ? View(result.Value)
                : View(new PlanCatalogue(Array.Empty<PublicPlan>(), Array.Empty<string>(), null));
        }

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var result = await _catalog.GetAsync(id, ct);
            if (!result.IsSuccess) return NotFound();

            // The comparison row under the detail page shows what else is on
            // offer; the same catalogue call keeps it consistent with /Plans.
            var catalogue = await _catalog.GetCatalogueAsync(null, ct);
            if (catalogue.IsSuccess && catalogue.Value is { } page)
            {
                ViewBag.Related = page.Plans
                    .Where(p => p.Id != id)
                    .Take(3)
                    .ToList();
            }

            return View(result.Value);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Catalog;

namespace NEXUS.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPlanCatalogService _catalog;

        public HomeController(IPlanCatalogService catalog) => _catalog = catalog;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var result = await _catalog.FeatureAsync(3, ct);

            IReadOnlyList<PublicPlan> plans = result.IsSuccess
                ? result.Value
                : Array.Empty<PublicPlan>();

            return View(new FeaturedPlans(plans));
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();
        public IActionResult Error() => View();
    }
}

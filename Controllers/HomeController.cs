using Microsoft.AspNetCore.Mvc;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Catalog;
using NEXUS.Services.Feedback;

namespace NEXUS.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPlanCatalogService _catalog;
        private readonly IContactMessageService _contact;

        public HomeController(IPlanCatalogService catalog, IContactMessageService contact)
        {
            _catalog = catalog;
            _contact = contact;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var result = await _catalog.FeatureAsync(3, ct);

            IReadOnlyList<PublicPlan> plans = result.IsSuccess
                ? result.Value
                : Array.Empty<PublicPlan>();

            return View(new FeaturedPlans(plans));
        }

        public IActionResult About() => View();

        [HttpGet]
        public IActionResult Contact() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessageForm form, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Check the highlighted fields and try again.";
                return View();
            }

            var result = await _contact.SubmitAsync(form, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message ?? "Your message could not be sent.";
                return View();
            }

            TempData["Success"] =
                "Thanks - your message has been received. Our team will reply within one working day.";
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Privacy() => View();
        public IActionResult Error() => View();
    }
}

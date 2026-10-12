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

            // Value is null on any failure, so the fallback covers both the
            // error case and an empty featured set with one expression.
            IReadOnlyList<PublicPlan> plans = result.Value ?? Array.Empty<PublicPlan>();

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

        /**
         * Every failure lands here: the exception handler re-executes this
         * action for a server fault, and the status-code middleware does the
         * same for the rest. The ORIGINAL status is kept so a crawler or a
         * fetch sees the true code rather than a 200.
         */
        [Route("Home/Error/{code:int?}")]
        public IActionResult Error(int? code)
        {
            var status = code ?? HttpContext.Response.StatusCode;
            if (status < 400) status = 500;

            Response.StatusCode = status;

            var page = ErrorPage.For(status);

            if (status >= 500)
            {
                page = new ErrorPage
                {
                    Code = page.Code,
                    Title = page.Title,
                    Message = page.Message,
                    Reference = HttpContext.TraceIdentifier
                };
            }

            return View("ErrorPage", page);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Feedback;

namespace NEXUS.Controllers
{
    [Authorize(Roles = NexusRoles.Customer + "," + NexusRoles.Admin)]
    public class FeedbackController : Controller
    {
        private readonly IFeedbackService _feedback;

        public FeedbackController(IFeedbackService feedback) => _feedback = feedback;

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            ViewData.SetActiveItem("feedback");

            var customerId = User.CustomerId();

            if (customerId is not int id)
            {
                ViewBag.History = Array.Empty<FeedbackRecord>();
                return View(new FeedbackViewModel());
            }

            var history = await _feedback.ForCustomerAsync(id, ct);
            ViewBag.History = history.IsSuccess
                ? history.Value!
                : Array.Empty<FeedbackRecord>();

            return View(new FeedbackViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FeedbackViewModel model, CancellationToken ct)
        {
            ViewData.SetActiveItem("feedback");

            var customerId = User.CustomerId();

            if (customerId is not int id)
            {
                ModelState.AddModelError(string.Empty,
                    "Only a signed-in customer can leave feedback.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                var history = await _feedback.ForCustomerAsync(id, ct);
                ViewBag.History = history.IsSuccess
                    ? history.Value!
                    : Array.Empty<FeedbackRecord>();
                return View(model);
            }

            var result = await _feedback.SubmitAsync(id, model, ct);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Could not save your feedback.");
                return View(model);
            }

            TempData["Success"] = "Thank you. Your feedback has been received.";
            return RedirectToAction(nameof(Index));
        }
    }
}

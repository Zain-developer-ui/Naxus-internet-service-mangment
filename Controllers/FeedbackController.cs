using Microsoft.AspNetCore.Mvc;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Api;

namespace NEXUS.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly IApiService _api;
        public FeedbackController(IApiService api) => _api = api;

        [HttpGet]
        public IActionResult Index() => View(new FeedbackViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FeedbackViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _api.SubmitFeedbackAsync(model);
            TempData["Success"] = "Thank you! Your feedback has been submitted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
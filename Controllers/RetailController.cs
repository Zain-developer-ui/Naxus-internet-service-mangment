using Microsoft.AspNetCore.Mvc;
using NEXUS.Models.ViewModels;

namespace NEXUS.Controllers
{
    public class RetailController : Controller
    {
        public IActionResult Dashboard() => View();

        [HttpGet]
        public IActionResult NewOrder() => View(new OrderViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NewOrder(OrderViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            TempData["Success"] = $"Retail order created for {model.FullName}.";
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Search() => View();
    }
}
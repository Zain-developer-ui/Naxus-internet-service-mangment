using Microsoft.AspNetCore.Mvc;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Api;

namespace NEXUS.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IApiService _api;
        public OrdersController(IApiService api) => _api = api;

        [HttpGet]
        public IActionResult New() => View(new OrderViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(OrderViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _api.SubmitOrderAsync(model);
            TempData["Success"] = "Order submitted successfully. Your Order ID is NX-ORD-1003.";
            return RedirectToAction(nameof(Tracking), new { id = "NX-ORD-1003" });
        }

        public async Task<IActionResult> Tracking(string? id)
        {
            ViewBag.OrderId = string.IsNullOrWhiteSpace(id) ? "NX-ORD-1001" : id;
            var order = await _api.GetOrderAsync(ViewBag.OrderId);
            return View(order);
        }
    }
}
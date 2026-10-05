using Microsoft.AspNetCore.Mvc;

namespace NEXUS.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard() => View();
        public IActionResult Reports() => View();
        public IActionResult Settings() => View();
    }
}
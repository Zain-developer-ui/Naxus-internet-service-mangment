using Microsoft.AspNetCore.Mvc;

namespace NEXUS.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult About() => View();
        public IActionResult Contact() => View();
        public IActionResult Error() => View();
    }
}
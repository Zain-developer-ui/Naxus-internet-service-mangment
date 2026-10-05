using Microsoft.AspNetCore.Mvc;
namespace NEXUS.Controllers
{
    public class TechnicalController : Controller
    {
        public IActionResult Dashboard() => View();
    }
}
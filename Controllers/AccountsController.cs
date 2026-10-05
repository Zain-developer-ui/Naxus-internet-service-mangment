using Microsoft.AspNetCore.Mvc;
namespace NEXUS.Controllers
{
    public class AccountsController : Controller
    {
        public IActionResult Dashboard() => View();
    }
}
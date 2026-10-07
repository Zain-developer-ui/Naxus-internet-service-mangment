using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Dashboards;

namespace NEXUS.Controllers
{
    [Authorize(Roles = NexusRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly IAdminDashboardService _dashboard;

        public AdminController(IAdminDashboardService dashboard) => _dashboard = dashboard;

        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            var result = await _dashboard.GetAsync(ct);

            ViewData.SetActiveItem("dashboard");

            return View(result.IsSuccess ? result.Value! : new AdminDashboardData());
        }

        public IActionResult Reports(CancellationToken ct)
        {
            ViewData.SetActiveItem("reports");
            return View();
        }

        public IActionResult Settings(CancellationToken ct)
        {
            ViewData.SetActiveItem("settings");
            return View();
        }
    }
}

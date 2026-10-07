using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Dashboards;

namespace NEXUS.Controllers
{
    [Authorize(Roles = NexusRoles.Technical + "," + NexusRoles.Admin)]
    public class TechnicalController : Controller
    {
        private readonly ITechnicalDashboardService _dash;

        public TechnicalController(ITechnicalDashboardService dash) => _dash = dash;

        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            ViewData.SetActiveItem("dashboard");
            var result = await _dash.GetAsync(ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new TechnicalDashboardData());
            }

            return View(result.Value!);
        }
    }
}

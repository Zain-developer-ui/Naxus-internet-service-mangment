using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Dashboards;

namespace NEXUS.Controllers
{
    // Accounts and Admin share this area; a technician or retail clerk has no
    // business seeing the ledger.
    [Authorize(Roles = NexusRoles.Accounts + "," + NexusRoles.Admin)]
    public class AccountsController : Controller
    {
        private readonly IAccountsDashboardService _dash;

        public AccountsController(IAccountsDashboardService dash) => _dash = dash;

        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            ViewData.SetActiveItem("dashboard");
            var result = await _dash.GetAsync(ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new AccountsDashboardData());
            }

            return View(result.Value!);
        }
    }
}

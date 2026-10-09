using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Dashboards;
using NEXUS.Services.Orders;
using NEXUS.Services.Reporting;
using NEXUS.Services.Settings;

namespace NEXUS.Controllers
{
    /**
     * Administrator-only. Reporting lives in ReportsController because every
     * staff console links to it, not just this one.
     */
    [Authorize(Roles = NexusRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly IAdminDashboardService _dashboard;
        private readonly IOrderRegisterService _orders;
        private readonly ISiteSettingsService _site;
        private readonly IReportService _reports;
        private readonly NexusDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminController(IAdminDashboardService dashboard, IOrderRegisterService orders,
                               ISiteSettingsService site, IReportService reports,
                               NexusDbContext db, IWebHostEnvironment env)
        {
            _dashboard = dashboard;
            _orders = orders;
            _site = site;
            _reports = reports;
            _db = db;
            _env = env;
        }

        /** Every order ever taken, including the closed ones. */
        public async Task<IActionResult> Orders(OrderStatus? status, ConnectionType? type,
                                                string? search, CancellationToken ct)
        {
            ViewData.SetActiveItem("orders");

            var result = await _orders.ListAsync(status, type, search, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new OrderRegisterPage());
            }

            return View(result.Value!);
        }

        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            var result = await _dashboard.GetAsync(ct);
            var data = result.IsSuccess ? result.Value! : new AdminDashboardData();

            // The trend comes from the same query the reports page runs, so the
            // two screens can never disagree about last month's revenue.
            var report = await _reports.BuildAsync(ReportFilter.Default(), ct);
            if (report.IsSuccess)
            {
                data.Trend = report.Value!.Monthly
                    .Select(p => new TrendPoint(p.Label, p.Revenue, p.Orders))
                    .ToList();
            }

            ViewData.SetActiveItem("admin-dashboard");

            return View(data);
        }

        /**
         * The website settings screen: name, branding, feature switches,
         * contact details and session policy. Values persist in SiteSettings;
         * anything not yet stored falls back to the catalogue default.
         */
        [HttpGet]
        public async Task<IActionResult> Settings(CancellationToken ct)
        {
            ViewData.SetActiveItem("settings");

            var page = await _site.GetAsync(ct);
            page.Runtime = await BuildRuntimeAsync(ct);

            return View(page);
        }

        /**
         * Reads the posted settings straight off the request rather than binding
         * an IFormCollection parameter - model binding of IFormCollection races
         * the antiforgery filter for the request body, and the filter wins with
         * a 400 before the action ever runs.
         */
        [HttpPost, ValidateAntiForgeryToken, ActionName("Settings")]
        public async Task<IActionResult> SaveSettings(CancellationToken ct)
        {
            ViewData.SetActiveItem("settings");

            var form = Request.Form;
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var definition in SiteSettingsCatalog.All)
            {
                if (definition.Kind == "bool")
                {
                    // An unticked checkbox sends nothing at all, so absence is the
                    // normal "off" case; a present "false" only arrives from a
                    // scripted client and should still mean off.
                    values[definition.Key] =
                        form.TryGetValue(definition.Key, out var flag)
                        && !string.Equals(flag.ToString(), "false", StringComparison.OrdinalIgnoreCase)
                            ? "true"
                            : "false";
                    continue;
                }

                if (form.TryGetValue(definition.Key, out var posted))
                    values[definition.Key] = posted.ToString();
            }

            var result = await _site.SaveAsync(values, ct);

            if (!result.IsSuccess)
            {
                foreach (var (field, messages) in result.Errors ?? new Dictionary<string, string[]>())
                    foreach (var message in messages)
                        ModelState.AddModelError(field, message);

                if (result.Errors is null)
                    ModelState.AddModelError(string.Empty, result.Message ?? "Could not save.");

                var failed = await _site.GetAsync(ct);
                failed.Runtime = await BuildRuntimeAsync(ct);
                return View(failed);
            }

            TempData["Success"] = "Website settings saved.";
            return RedirectToAction(nameof(Settings));
        }

        private async Task<SystemRuntime> BuildRuntimeAsync(CancellationToken ct)
        {
            var start = System.Diagnostics.Process.GetCurrentProcess().StartTime;

            return new SystemRuntime
            {
                Environment = _env.EnvironmentName,
                DatabaseName = _db.Database.GetDbConnection().Database,
                ServerTime = DateTime.Now.ToString("dd MMM yyyy, HH:mm"),
                Uptime = FormatUptime(DateTime.Now - start),
                Customers = await _db.Customers.CountAsync(ct),
                ActiveConnections = await _db.Connections.CountAsync(
                    c => c.Status == ConnectionStatus.Active, ct)
            };
        }

        private static string FormatUptime(TimeSpan span)
        {
            if (span.TotalMinutes < 1) return "under a minute";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} minutes";

            var hours = (int)span.TotalHours;
            var minutes = span.Minutes;

            return minutes == 0 ? $"{hours} hours" : $"{hours}h {minutes}m";
        }
    }
}

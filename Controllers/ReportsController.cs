using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Services.Reporting;

namespace NEXUS.Controllers;

/**
 * Management reporting for every staff console, not just the administrator.
 *
 * Retail, technical and accounts all have a Reports entry in their sidebar,
 * and each of them already sees the underlying customers, orders and bills
 * through their own pages - so the report is not a wider disclosure than the
 * role already has. Keeping it out of AdminController means that controller
 * can stay strictly administrator-only.
 */
[Authorize(Roles = NexusRoles.StaffRoles)]
public class ReportsController : Controller
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    public async Task<IActionResult> Index(DateTime? from, DateTime? to,
                                           ConnectionType? service, CancellationToken ct)
    {
        ViewData.SetActiveItem("reports");

        var fallback = ReportFilter.Default();
        var filter = new ReportFilter(
            From: (from ?? fallback.From).Date,
            To: (to ?? fallback.To).Date,
            Service: service);

        var result = await _reports.BuildAsync(filter, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Message;
            return View("Index", new ReportPage { Filter = fallback });
        }

        return View(result.Value!);
    }

    /**
     * Same figures as the page, flattened to CSV so the numbers can leave the
     * system. The customer table is the one management asks for.
     */
    public async Task<IActionResult> ExportCustomers(DateTime? from, DateTime? to,
                                                     ConnectionType? service, CancellationToken ct)
    {
        var fallback = ReportFilter.Default();
        var filter = new ReportFilter((from ?? fallback.From).Date,
                                      (to ?? fallback.To).Date, service);

        var result = await _reports.BuildAsync(filter, ct);
        if (!result.IsSuccess) return BadRequest(result.Message);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("AccountId,Name,City,Service,Plan,Status,Registered,MonthlyRate");

        foreach (var row in result.Value!.Customers)
            csv.AppendLine(string.Join(',',
                Quote(row.AccountId), Quote(row.Name), Quote(row.City), Quote(row.ConnectionType),
                Quote(row.Plan), Quote(row.Status), row.RegisteredOn.ToString("yyyy-MM-dd"),
                row.MonthlyRate.ToString("0.00")));

        var name = $"nexus-customers-{filter.From:yyyyMMdd}-{filter.To:yyyyMMdd}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", name);
    }

    private static string Quote(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}

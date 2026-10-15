using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Accounts;
using NEXUS.Services.Billing;
using NEXUS.Services.Dashboards;
using NEXUS.Services.Settings;

namespace NEXUS.Controllers
{
    // Accounts and Admin share this area; a technician or retail clerk has no
    // business seeing the ledger.
    [Authorize(Roles = NexusRoles.Accounts + "," + NexusRoles.Admin)]
    public class AccountsController : Controller
    {
        private readonly IAccountsDashboardService _dash;
        private readonly ICallMeteringService _calls;
        private readonly IBillingService _billing;
        private readonly ISiteSettingsService _settings;

        public AccountsController(IAccountsDashboardService dash, ICallMeteringService calls,
                                  IBillingService billing, ISiteSettingsService settings)
        {
            _dash = dash;
            _calls = calls;
            _billing = billing;
            _settings = settings;
        }

        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            ViewData.SetActiveItem("dashboard", "billing");
            var result = await _dash.GetAsync(ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new AccountsDashboardData());
            }

            return View(result.Value!);
        }

        [HttpGet]
        public async Task<IActionResult> Calls(CancellationToken ct)
        {
            ViewData.SetActiveItem("calls");
            return View(await LoadPageAsync(ct));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Calls(CallMeterInput input, CancellationToken ct)
        {
            ViewData.SetActiveItem("calls");

            if (!ModelState.IsValid)
                return View(await LoadPageAsync(ct, input));

            var result = await _calls.SaveAsync(input, ct);
            if (!result.IsSuccess)
            {
                ApplyErrors(result);
                return View(await LoadPageAsync(ct, input));
            }

            TempData["Success"] = "Call reading saved. The next bill for this period will carry the charges.";
            return RedirectToAction(nameof(Calls));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCall(int id, CancellationToken ct)
        {
            var result = await _calls.DeleteAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Call reading removed."
                : result.Message;

            return RedirectToAction(nameof(Calls));
        }

        /**
         * One-off charges waiting to be pulled onto a bill - the SRS's
         * replacement charge when a customer damages equipment, and manual
         * adjustments accounts need. They are raised here rather than sold,
         * because the SRS never prices equipment.
         */
        [HttpGet]
        public async Task<IActionResult> Charges(int? connectionId, CancellationToken ct)
        {
            ViewData.SetActiveItem("charges");

            var result = await _billing.PendingChargesAsync(connectionId, ct);
            var page = await LoadChargePageAsync(result, connectionId, ct);

            return View(page);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Charges(RaiseChargeInput input, CancellationToken ct)
        {
            ViewData.SetActiveItem("charges");

            if (!ModelState.IsValid)
            {
                var invalid = await _billing.PendingChargesAsync(null, ct);
                return View(await LoadChargePageAsync(invalid, null, ct, input));
            }

            var result = await _billing.RaiseChargeAsync(input.ConnectionId, input.Description,
                input.ChargeType, input.Amount, User.EmployeeId(), ct);

            if (!result.IsSuccess)
            {
                foreach (var (field, messages) in result.Errors ?? new Dictionary<string, string[]>())
                    foreach (var message in messages)
                        ModelState.AddModelError(field, message);

                if (result.Errors is null) ModelState.AddModelError(string.Empty, result.Message ?? "Could not raise the charge.");

                var failed = await _billing.PendingChargesAsync(null, ct);
                return View(await LoadChargePageAsync(failed, null, ct, input));
            }

            TempData["Success"] = "Charge raised. It will appear on the customer's next bill.";
            return RedirectToAction(nameof(Charges));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelCharge(int id, CancellationToken ct)
        {
            var result = await _billing.CancelChargeAsync(id, ct);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
                ? "Charge withdrawn."
                : result.Message;

            return RedirectToAction(nameof(Charges));
        }

        /**
         * The monthly cycle. A period runs month-to-date; the run is idempotent
         * because a connection that already has a bill for the window is
         * skipped, which is why staff can press it twice without harm.
         */
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RunBilling(string? period, CancellationToken ct)
        {
            if (!await _settings.GetFlagAsync("feature.autoInvoice", ct))
            {
                TempData["Error"] = "Automatic billing is switched off in Settings - " +
                                    "raise each bill by hand from the Bills screen.";
                return RedirectToAction(nameof(Dashboard));
            }

            var start = ParseMonth(period);
            var end = start.AddMonths(1);

            var result = await _billing.GenerateForPeriodAsync(start, end, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message ?? "The billing run could not be completed.";
                return RedirectToAction(nameof(Dashboard));
            }

            var outcome = result.Value!;
            TempData[outcome.Created > 0 ? "Success" : "Error"] = outcome.Created > 0
                ? $"Billing run for {start:MMM yyyy}: {outcome.Created} bill(s) drafted, {outcome.Skipped} skipped."
                : $"Nothing to bill for {start:MMM yyyy} - {outcome.Skipped} connection(s) were skipped.";

            return RedirectToAction(nameof(Dashboard));
        }

        private static DateTime ParseMonth(string? raw)
        {
            if (DateTime.TryParse(raw + "-01", out var parsed))
                return new DateTime(parsed.Year, parsed.Month, 1);

            var now = DateTime.UtcNow;
            return new DateTime(now.Year, now.Month, 1);
        }

        private async Task<ChargePage> LoadChargePageAsync(Result<IReadOnlyList<PendingCharge>> result,
                                                           int? connectionId, CancellationToken ct,
                                                           RaiseChargeInput? input = null)
        {
            var page = new ChargePage { FilterConnectionId = connectionId, Input = input ?? new RaiseChargeInput() };

            var connections = await _billing.ChargeableConnectionsAsync(ct);
            page.Connections = connections.IsSuccess ? connections.Value! : Array.Empty<ChargeableConnection>();

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return page;
            }

            page.Charges = result.Value!;
            return page;
        }

        private async Task<CallMeteringPage> LoadPageAsync(CancellationToken ct, CallMeterInput? input = null)
        {
            var result = await _calls.LoadAsync(ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return new CallMeteringPage();
            }

            if (input is not null) result.Value!.Input = input;
            return result.Value!;
        }

        private void ApplyErrors(Result<int> result)
        {
            if (result.Errors is null)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Could not save the reading.");
                return;
            }

            foreach (var (field, messages) in result.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(field, message);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Feasibility;
using NEXUS.Services.Lifecycle;

namespace NEXUS.Controllers
{
    /**
     * The operations console: survey the queue, drive an order through
     * installation, then manage the live connections it created.
     */
    [Authorize(Roles = NexusRoles.Technical + "," + NexusRoles.Admin + "," + NexusRoles.Accounts)]
    public class OperationsController : Controller
    {
        private readonly IFeasibilityService _ops;
        private readonly IConnectionLifecycleService _lifecycle;

        public OperationsController(IFeasibilityService ops, IConnectionLifecycleService lifecycle)
        {
            _ops = ops;
            _lifecycle = lifecycle;
        }

        // ------------------------------------------------------------ queue

        public async Task<IActionResult> Index(OrderStatus? status, CancellationToken ct)
        {
            ViewData.SetActiveItem("operations");

            var result = await _ops.GetQueueAsync(status, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new OrderQueuePage(Array.Empty<OrderQueueItem>(), status));
            }

            return View(new OrderQueuePage(result.Value!, status));
        }

        public async Task<IActionResult> Review(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("operations");

            var result = await _ops.GetOrderAsync(id, ct);
            if (!result.IsSuccess) return NotFound();
            return View(result.Value);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordCheck(FeasibilityEntry form, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Check the survey values and try again.";
                return RedirectToAction(nameof(Review), new { id = form.OrderId });
            }

            var result = await _ops.RecordCheckAsync(form, User.EmployeeId(), ct);
            Announce(result, "Survey recorded.");
            return RedirectToAction(nameof(Review), new { id = form.OrderId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ConfirmOrderEntry form, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Pick a valid installation date.";
                return RedirectToAction(nameof(Review), new { id = form.OrderId });
            }

            var result = await _ops.ConfirmAsync(form.OrderId, form.ScheduledFor, User.EmployeeId(), ct);
            Announce(result, "Order confirmed.");
            return RedirectToAction(nameof(Review), new { id = form.OrderId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StartInstallation(int orderId, CancellationToken ct)
        {
            var result = await _ops.StartInstallationAsync(orderId, User.EmployeeId(), ct);
            Announce(result, "Installation started.");
            return RedirectToAction(nameof(Review), new { id = orderId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteInstallation(int orderId, CancellationToken ct)
        {
            var result = await _ops.CompleteInstallationAsync(orderId, User.EmployeeId(), ct);
            Announce(result, "Installation completed and line created.");
            return RedirectToAction(nameof(Review), new { id = orderId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(CancelOrderEntry form, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Give a reason before cancelling.";
                return RedirectToAction(nameof(Review), new { id = form.OrderId });
            }

            var result = await _ops.CancelOrderAsync(form.OrderId, form.Reason, User.EmployeeId(), ct);
            Announce(result, "Order cancelled.");
            return RedirectToAction(nameof(Review), new { id = form.OrderId });
        }

        // ------------------------------------------------------------ connections

        public async Task<IActionResult> Connections(ConnectionStatus? status, string? search, CancellationToken ct)
        {
            ViewData.SetActiveItem("connections");

            var result = await _ops.GetConnectionsAsync(status, search, ct);
            var totals = await _ops.GetConnectionTotalsAsync(ct);

            var registerTotals = totals.IsSuccess
                ? totals.Value!
                : new ConnectionRegisterTotals(0, 0, 0, 0, 0);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new ConnectionListPage(
                    Array.Empty<ConnectionRow>(), status, search, registerTotals));
            }

            return View(new ConnectionListPage(result.Value!, status, search, registerTotals));
        }

        public async Task<IActionResult> Connection(int id, CancellationToken ct)
        {
            ViewData.SetActiveItem("connections");

            var result = await _ops.GetConnectionAsync(id, ct);
            if (!result.IsSuccess) return NotFound();
            return View(result.Value);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(ConnectionStatusEntry form, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Choose a status and give a reason.";
                return RedirectToAction(nameof(Connection), new { id = form.ConnectionId });
            }

            var result = await _ops.ChangeStatusAsync(form.ConnectionId, form.Target, form.Reason, User.EmployeeId(), ct);
            Announce(result, "Connection status updated.");
            return RedirectToAction(nameof(Connection), new { id = form.ConnectionId });
        }

        // ------------------------------------------------- overdue policy

        /**
         * The postpaid rule from the SRS: an unpaid bill eventually costs the
         * customer the line. The preview is what the sweep would do if it ran,
         * which is why the same call backs the apply button.
         */
        public async Task<IActionResult> Overdue(CancellationToken ct)
        {
            ViewData.SetActiveItem("overdue");

            var result = await _lifecycle.PreviewAsync(ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new OverduePolicyResult(Array.Empty<OverdueAction>(), 0, DateTime.UtcNow.Date));
            }

            return View(result.Value!);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyOverdue(CancellationToken ct)
        {
            var result = await _lifecycle.ApplyAsync(User.EmployeeId(), ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message ?? "The policy could not be applied.";
                return RedirectToAction(nameof(Overdue));
            }

            var plan = result.Value!;
            TempData["Success"] = plan.Actions.Count == 0
                ? "Nothing to do - no line is past its payment window."
                : $"{plan.Suspensions} line(s) suspended and {plan.Closures} closed.";

            return RedirectToAction(nameof(Overdue));
        }

        private void Announce<T>(Common.Result<T> result, string success)
        {
            if (result.IsSuccess)
            {
                TempData["Success"] = success;
            }
            else
            {
                TempData["Error"] = result.Message
                    ?? result.Errors?.Values.SelectMany(v => v).FirstOrDefault()
                    ?? "That change could not be applied.";
            }
        }
    }
}

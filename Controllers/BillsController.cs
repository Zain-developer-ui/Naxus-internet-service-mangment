using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Billing;

namespace NEXUS.Controllers;

/**
 * Customers see only their own bills; accounts and admin see the ledger.
 * The split is decided per action rather than by two controllers, because the
 * same bill view serves both - only the amount of it differs.
 */
[Authorize(Roles = NexusRoles.Customer + "," + NexusRoles.Accounts + "," +
                   NexusRoles.Admin + "," + NexusRoles.Retail)]
public class BillsController : Controller
{
    private readonly IBillingService _billing;
    private readonly ILogger<BillsController> _log;

    public BillsController(IBillingService billing, ILogger<BillsController> log)
    {
        _billing = billing;
        _log = log;
    }

    public async Task<IActionResult> Index(BillStatus? status, string? search,
                                           CancellationToken ct)
    {
        var isCustomer = User.IsInRole(NexusRoles.Customer);

        var query = new BillQuery(
            CustomerId: isCustomer ? User.CustomerId() : null,
            Status: status,
            Search: isCustomer ? null : search);

        var result = await _billing.ListAsync(query, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Message;
            return View(new BillListPage
            {
                IsCustomerView = isCustomer,
                Filter = status
            });
        }

        var page = new BillListPage
        {
            Bills = result.Value!,
            IsCustomerView = isCustomer,
            Filter = status,
            Search = search
        };

        return View(page);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await _billing.GetAsync(id, ct);
        if (!result.IsSuccess) return NotFound();

        if (User.IsInRole(NexusRoles.Customer))
        {
            var own = User.CustomerId();
            var owner = await _billing.ForCustomerAsync(own ?? 0, ct);
            var belongs = owner.IsSuccess && owner.Value!.Any(b => b.Id == id);
            if (!belongs) return Forbid();
        }

        var payments = await _billing.PaymentsForAsync(id, ct);

        return View(new BillDetailPage
        {
            Bill = result.Value!,
            Payments = payments.IsSuccess ? payments.Value! : Array.Empty<PaymentRecord>(),
            CanRecordPayment = !User.IsInRole(NexusRoles.Customer)
        });
    }

    /** Accounts-side preview before a bill is committed. */
    [Authorize(Roles = NexusRoles.Accounts + "," + NexusRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(PaymentEntry form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Check the payment details and try again.";
            return RedirectToAction(nameof(Details), new { id = form.BillId });
        }

        var result = await _billing.RecordPaymentAsync(
            form.BillId, form.Amount, form.Method, form.Reference, form.Remarks,
            shopId: User.ShopId(), userId: User.UserId(), ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Errors?.Values.FirstOrDefault()?.FirstOrDefault()
                                ?? result.Message;
            return RedirectToAction(nameof(Details), new { id = form.BillId });
        }

        TempData["Success"] =
            $"Receipt {result.Value!.Payment.ReceiptNumber} recorded for {result.Value.Payment.Amount:C}.";

        return RedirectToAction(nameof(Details), new { id = form.BillId });
    }
}

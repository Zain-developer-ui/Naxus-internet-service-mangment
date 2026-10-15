using Microsoft.EntityFrameworkCore;
using NEXUS.Common.Constants;
using NEXUS.Data;

namespace NEXUS.Services.Notifications;

/**
 * What the topbar bell shows. Every entry is counted from live rows rather than
 * stored as a message, so the badge cannot drift from the work that is actually
 * outstanding and nothing has to be marked as read.
 */
public sealed record NotificationItem(string Title, string Detail, string Url, string Tone);

public interface INotificationService
{
    Task<IReadOnlyList<NotificationItem>> ForAsync(string role, int? customerId,
                                                   CancellationToken ct = default);
}

public sealed class NotificationService : INotificationService
{
    private readonly NexusDbContext _db;

    public NotificationService(NexusDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationItem>> ForAsync(string role, int? customerId,
                                                                CancellationToken ct = default)
    {
        var items = new List<NotificationItem>();

        if (role is NexusRoles.Admin or NexusRoles.Technical)
        {
            var surveys = await _db.ConnectionOrders
                .CountAsync(o => o.Status == OrderStatus.AwaitingFeasibility, ct);

            if (surveys > 0)
                items.Add(new NotificationItem(
                    "Surveys waiting",
                    $"{surveys} order{(surveys == 1 ? "" : "s")} still need a feasibility check.",
                    "/Operations", "warning"));
        }

        if (role is NexusRoles.Admin or NexusRoles.Accounts)
        {
            var overdue = await _db.Bills
                .CountAsync(b => b.Status == BillStatus.Overdue, ct);

            if (overdue > 0)
                items.Add(new NotificationItem(
                    "Overdue bills",
                    $"{overdue} bill{(overdue == 1 ? "" : "s")} past the due date.",
                    "/Bills?status=Overdue", "danger"));

            var charges = await _db.ConnectionCharges
                .CountAsync(c => c.BilledOnBillId == null, ct);

            if (charges > 0)
                items.Add(new NotificationItem(
                    "Charges not billed yet",
                    $"{charges} one-off charge{(charges == 1 ? "" : "s")} waiting for the next bill.",
                    "/Accounts/Charges", "info"));
        }

        if (role is NexusRoles.Admin or NexusRoles.Retail)
        {
            var low = await _db.EquipmentProducts
                .CountAsync(p => p.IsActive &&
                                 p.Stock.Sum(s => (int?)s.Quantity) < p.LowStockThreshold, ct);

            if (low > 0)
                items.Add(new NotificationItem(
                    "Stock running low",
                    $"{low} item{(low == 1 ? "" : "s")} at or below the reorder level.",
                    "/Admin/Inventory", "warning"));
        }

        if (role == NexusRoles.Customer && customerId is int id)
        {
            var due = await _db.Bills
                .CountAsync(b => b.CustomerId == id &&
                                 (b.Status == BillStatus.Issued || b.Status == BillStatus.Overdue), ct);

            if (due > 0)
                items.Add(new NotificationItem(
                    "Bills to pay",
                    $"{due} bill{(due == 1 ? "" : "s")} awaiting payment.",
                    "/Bills", "danger"));
        }

        return items;
    }
}

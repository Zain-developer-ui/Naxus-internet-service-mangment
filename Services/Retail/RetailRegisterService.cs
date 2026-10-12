using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Retail;

public interface IRetailRegisterService
{
    Task<Result<RetailRegisterPage>> BuildAsync(RetailRegisterQuery query, CancellationToken ct = default);
}

/**
 * The four registers the SRS puts on a retail outlet's screen: the orders placed
 * to date, the connections they became, the bills raised against those lines,
 * and the payments taken. One period drives all four so the figures reconcile -
 * an order counted here is a line counted here is a bill counted here.
 */
public sealed class RetailRegisterService : IRetailRegisterService
{
    private const int RowCap = 200;

    private readonly NexusDbContext _db;

    public RetailRegisterService(NexusDbContext db) => _db = db;

    public async Task<Result<RetailRegisterPage>> BuildAsync(
        RetailRegisterQuery query, CancellationToken ct = default)
    {
        var from = query.From?.Date;
        var to = query.To?.Date.AddDays(1);

        var orders = await BuildOrdersAsync(from, to, ct);
        var connections = await BuildConnectionsAsync(from, to, ct);
        var bills = await BuildBillsAsync(from, to, ct);
        var payments = await BuildPaymentsAsync(from, to, ct);

        return Result<RetailRegisterPage>.Ok(new RetailRegisterPage
        {
            Query = query,
            Orders = orders,
            Connections = connections,
            Bills = bills,
            Payments = payments,
            Totals = new RetailRegisterTotals(
                orders.Count,
                connections.Count,
                bills.Sum(b => b.Total),
                payments.Sum(p => p.Amount),
                bills.Sum(b => b.Balance))
        });
    }

    private async Task<IReadOnlyList<SearchOrderRow>> BuildOrdersAsync(
        DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .AsQueryable();

        if (from is not null) q = q.Where(o => o.CreatedAt >= from);
        if (to is not null) q = q.Where(o => o.CreatedAt < to);

        return await q
            .OrderByDescending(o => o.CreatedAt)
            .Take(RowCap)
            .Select(o => new SearchOrderRow(
                o.OrderId,
                o.Customer!.FullName,
                o.ConnectionType.DisplayName(),
                o.Plan!.Name,
                o.Status.DisplayName(),
                o.Status.ToString(),
                o.CreatedAt))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<SearchConnectionRow>> BuildConnectionsAsync(
        DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = _db.Connections
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Where(c => !c.IsDeleted);

        if (from is not null) q = q.Where(c => c.ActivatedOn >= from);
        if (to is not null) q = q.Where(c => c.ActivatedOn < to);

        return await q
            .OrderByDescending(c => c.ActivatedOn)
            .Take(RowCap)
            .Select(c => new SearchConnectionRow(
                c.Id,
                c.ConnectionNumber,
                c.Customer!.AccountId,
                c.Customer.FullName,
                c.ConnectionType.DisplayName(),
                c.Plan!.Name,
                c.Status.DisplayName(),
                c.Status.ToString(),
                c.ActivatedOn,
                c.City!.Name))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<RegisterBillRow>> BuildBillsAsync(
        DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = _db.Bills
            .AsNoTracking()
            .Include(b => b.Customer)
            .Where(b => b.Status != BillStatus.Cancelled);

        // A bill belongs to the period it covers, not the day it was printed.
        if (from is not null) q = q.Where(b => b.PeriodStart >= from);
        if (to is not null) q = q.Where(b => b.PeriodStart < to);

        return await q
            .OrderByDescending(b => b.IssuedOn)
            .Take(RowCap)
            .Select(b => new RegisterBillRow(
                b.Id,
                b.BillNumber,
                b.Customer!.FullName,
                b.Customer.AccountId,
                b.PeriodStart.ToString("dd MMM") + " - " + b.PeriodEnd.ToString("dd MMM yyyy"),
                b.TotalAmount,
                b.AmountPaid,
                b.TotalAmount - b.AmountPaid,
                b.Status.DisplayName(),
                b.Status.ToString(),
                b.DueOn))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<RegisterPaymentRow>> BuildPaymentsAsync(
        DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = _db.Payments
            .AsNoTracking()
            .Include(p => p.Bill)
            .Include(p => p.Customer)
            .AsQueryable();

        if (from is not null) q = q.Where(p => p.PaidOn >= from);
        if (to is not null) q = q.Where(p => p.PaidOn < to);

        return await q
            .OrderByDescending(p => p.PaidOn)
            .Take(RowCap)
            .Select(p => new RegisterPaymentRow(
                p.ReceiptNumber,
                p.Bill!.BillNumber,
                p.Customer!.FullName,
                p.Amount,
                p.Method.DisplayName(),
                p.PaidOn))
            .ToListAsync(ct);
    }
}

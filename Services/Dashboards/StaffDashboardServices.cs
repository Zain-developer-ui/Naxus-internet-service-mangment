using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Domain.Billing;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Dashboards;

public interface IAccountsDashboardService
{
    Task<Result<AccountsDashboardData>> GetAsync(CancellationToken ct = default);
}

/**
 * Real ledger figures for the accounts console. The collection rate is
 * collected-over-billed for everything ever issued, not a hardcoded constant -
 * on an empty database it reads 0% and that is the honest answer.
 */
public sealed class AccountsDashboardService : IAccountsDashboardService
{
    private readonly NexusDbContext _db;

    public AccountsDashboardService(NexusDbContext db) => _db = db;

    public async Task<Result<AccountsDashboardData>> GetAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var data = new AccountsDashboardData
        {
            PaidBills = await _db.Bills.CountAsync(b => b.Status == BillStatus.Paid, ct),
            PendingBills = await _db.Bills
                .CountAsync(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid, ct),
            OverdueBills = await _db.Bills.CountAsync(b => b.Status == BillStatus.Overdue, ct),
            TotalCustomers = await _db.Customers.CountAsync(c => !c.IsDeleted, ct)
        };

        data.CollectedToday = await _db.Payments
            .Where(p => p.PaidOn >= today)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        data.CollectedThisMonth = await _db.Payments
            .Where(p => p.PaidOn >= monthStart)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        data.OutstandingAmount = await _db.Bills
            .Where(b => b.Status == BillStatus.Issued ||
                        b.Status == BillStatus.PartiallyPaid ||
                        b.Status == BillStatus.Overdue)
            .SumAsync(b => (decimal?)(b.TotalAmount - b.AmountPaid), ct) ?? 0m;

        data.BilledThisCycle = await _db.Bills
            .SumAsync(b => (decimal?)b.TotalAmount, ct) ?? 0m;

        var collectedAllTime = await _db.Payments
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        data.CollectionRatePercent = data.BilledThisCycle > 0m
            ? Math.Round(collectedAllTime / data.BilledThisCycle * 100m, 1)
            : 0m;

        data.RecentPayments = await _db.Payments
            .AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Bill)
            .OrderByDescending(p => p.PaidOn)
            .Take(6)
            .Select(p => new AccountsPaymentRow(
                p.ReceiptNumber,
                p.Customer!.FullName,
                p.Customer.AccountId,
                p.Bill!.BillNumber,
                p.Amount,
                p.Method.DisplayName(),
                p.PaidOn))
            .ToListAsync(ct);

        data.Outstanding = await _db.Bills
            .AsNoTracking()
            .Include(b => b.Customer)
            .Where(b => b.Status == BillStatus.Issued ||
                        b.Status == BillStatus.PartiallyPaid ||
                        b.Status == BillStatus.Overdue)
            .OrderBy(b => b.DueOn)
            .Take(6)
            .Select(b => new AccountsBillRow(
                b.BillNumber,
                b.Customer!.FullName,
                b.Customer.AccountId,
                b.TotalAmount - b.AmountPaid,
                b.DueOn,
                b.Status.DisplayName(),
                b.Status.ToString()))
            .ToListAsync(ct);

        return Result<AccountsDashboardData>.Ok(data);
    }
}

public interface ITechnicalDashboardService
{
    Task<Result<TechnicalDashboardData>> GetAsync(CancellationToken ct = default);
}

/**
 * Field overview. "Network healthy" on the old screen was a literal string; the
 * nearest honest equivalent is the count of lines that are actually active.
 */
public sealed class TechnicalDashboardService : ITechnicalDashboardService
{
    private readonly NexusDbContext _db;

    public TechnicalDashboardService(NexusDbContext db) => _db = db;

    public async Task<Result<TechnicalDashboardData>> GetAsync(CancellationToken ct = default)
    {
        var live = _db.Connections.AsNoTracking().Where(c => !c.IsDeleted);

        var data = new TechnicalDashboardData
        {
            ActiveConnections = await live.CountAsync(c => c.Status == ConnectionStatus.Active, ct),
            SuspendedLines = await live.CountAsync(c => c.Status == ConnectionStatus.TemporarilyInactive, ct),
            InstallationQueue = await _db.ConnectionOrders
                .CountAsync(o => o.Status == OrderStatus.Confirmed ||
                                 o.Status == OrderStatus.Installing, ct),
            CompletedInstallations = await _db.ConnectionOrders
                .CountAsync(o => o.Status == OrderStatus.Completed, ct)
        };

        data.Queue = await _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.City)
            .Where(o => o.Status == OrderStatus.Confirmed ||
                        o.Status == OrderStatus.Installing ||
                        o.Status == OrderStatus.FeasibilityPassed)
            .OrderBy(o => o.CreatedAt)
            .Take(8)
            .Select(o => new TechnicalQueueRow(
                o.OrderId,
                o.Customer!.FullName,
                o.Customer.AccountId,
                o.Plan!.Name,
                o.ConnectionType.DisplayName(),
                o.City!.Name,
                o.Status.DisplayName(),
                o.Status.ToString(),
                o.CreatedAt))
            .ToListAsync(ct);

        data.RecentConnections = await live
            .Include(c => c.Customer)
            .OrderByDescending(c => c.CreatedAt)
            .Take(8)
            .Select(c => new TechnicalConnectionRow(
                c.ConnectionNumber,
                c.Customer!.FullName,
                c.ConnectionType.DisplayName(),
                c.Status.DisplayName(),
                c.Status.ToString(),
                c.ActivatedOn))
            .ToListAsync(ct);

        return Result<TechnicalDashboardData>.Ok(data);
    }
}

public interface IRetailDashboardService
{
    Task<Result<RetailDashboardData>> GetAsync(CancellationToken ct = default);
}

/**
 * Front-desk counters for a branch clerk. Week starts Monday, which is how the
 * competition's own reports treat it.
 */
public sealed class RetailDashboardService : IRetailDashboardService
{
    private readonly NexusDbContext _db;

    public RetailDashboardService(NexusDbContext db) => _db = db;

    public async Task<Result<RetailDashboardData>> GetAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        var data = new RetailDashboardData
        {
            NewCustomersToday = await _db.Customers.CountAsync(c => !c.IsDeleted && c.CreatedAt >= today, ct),
            OrdersToday = await _db.ConnectionOrders.CountAsync(o => o.CreatedAt >= today, ct),
            OrdersThisWeek = await _db.ConnectionOrders.CountAsync(o => o.CreatedAt >= weekStart, ct),
            PendingOrders = await _db.ConnectionOrders
                .CountAsync(o => o.Status == OrderStatus.AwaitingFeasibility ||
                                 o.Status == OrderStatus.FeasibilityPassed ||
                                 o.Status == OrderStatus.Confirmed ||
                                 o.Status == OrderStatus.Installing, ct),
            CompletedOrders = await _db.ConnectionOrders
                .CountAsync(o => o.Status == OrderStatus.Completed, ct)
        };

        data.SalesThisWeek = await _db.ConnectionOrders
            .Where(o => o.CreatedAt >= weekStart)
            .SumAsync(o => (decimal?)o.QuotedAmount, ct) ?? 0m;

        data.RecentOrders = await _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.City)
            .OrderByDescending(o => o.CreatedAt)
            .Take(8)
            .Select(o => new RetailOrderRow(
                o.OrderId,
                o.Customer!.FullName,
                o.Customer.AccountId,
                o.Plan!.Name,
                o.ConnectionType.DisplayName(),
                o.City!.Name,
                o.Status.DisplayName(),
                o.Status.ToString(),
                o.CreatedAt))
            .ToListAsync(ct);

        return Result<RetailDashboardData>.Ok(data);
    }
}

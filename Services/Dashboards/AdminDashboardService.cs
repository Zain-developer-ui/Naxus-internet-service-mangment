using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Domain.Billing;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Dashboards;

public interface IAdminDashboardService
{
    Task<Result<AdminDashboardData>> GetAsync(CancellationToken ct = default);
}

/**
 * Live figures for the admin overview. Every number here is counted from the
 * database - the previous screen was driven by a fixture with invented
 * customers, invoices and service types ("Fiber", "Wireless") that this system
 * does not even sell.
 */
public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly NexusDbContext _db;

    public AdminDashboardService(NexusDbContext db) => _db = db;

    public async Task<Result<AdminDashboardData>> GetAsync(CancellationToken ct = default)
    {
        var data = new AdminDashboardData
        {
            TotalCustomers = await _db.Customers.CountAsync(c => !c.IsDeleted, ct),
            ActiveConnections = await _db.Connections
                .CountAsync(c => !c.IsDeleted && c.Status == ConnectionStatus.Active, ct),
            PendingOrders = await _db.ConnectionOrders
                .CountAsync(o => o.Status == OrderStatus.AwaitingFeasibility ||
                                 o.Status == OrderStatus.FeasibilityPassed ||
                                 o.Status == OrderStatus.Confirmed ||
                                 o.Status == OrderStatus.Installing, ct),
            ActiveInstallations = await _db.ConnectionOrders
                .CountAsync(o => o.Status == OrderStatus.Installing, ct),
            OutstandingBills = await _db.Bills
                .CountAsync(b => b.Status == BillStatus.Issued ||
                                 b.Status == BillStatus.PartiallyPaid ||
                                 b.Status == BillStatus.Overdue, ct),
            SuccessfulPayments = await _db.Payments.CountAsync(ct)
        };

        data.MonthlyRevenue = await _db.Payments
            .Where(p => p.PaidOn.Year == DateTime.UtcNow.Year &&
                        p.PaidOn.Month == DateTime.UtcNow.Month)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        data.OutstandingAmount = await _db.Bills
            .Where(b => b.Status == BillStatus.Issued ||
                        b.Status == BillStatus.PartiallyPaid ||
                        b.Status == BillStatus.Overdue)
            .SumAsync(b => (decimal?)(b.TotalAmount - b.AmountPaid), ct) ?? 0m;

        // SRS service mix - the three types the network actually offers.
        data.BroadbandConnections = await _db.Connections
            .CountAsync(c => !c.IsDeleted && c.ConnectionType == ConnectionType.Broadband, ct);
        data.DialUpConnections = await _db.Connections
            .CountAsync(c => !c.IsDeleted && c.ConnectionType == ConnectionType.DialUp, ct);
        data.TelephoneConnections = await _db.Connections
            .CountAsync(c => !c.IsDeleted && c.ConnectionType == ConnectionType.Telephone, ct);

        data.RecentOrders = await _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.City)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOrderRow(
                o.OrderId,
                o.Customer!.FullName,
                o.Customer.AccountId,
                o.Plan!.Name,
                o.City!.Name,
                o.Status.DisplayName(),
                o.Status.ToString(),
                o.CreatedAt))
            .ToListAsync(ct);

        data.RecentPayments = await _db.Payments
            .AsNoTracking()
            .Include(p => p.Customer)
            .OrderByDescending(p => p.PaidOn)
            .Take(5)
            .Select(p => new RecentPaymentRow(
                p.Customer!.FullName,
                p.ReceiptNumber,
                p.Amount,
                p.Method.DisplayName(),
                p.PaidOn))
            .ToListAsync(ct);

        data.RecentCustomers = await _db.Customers
            .AsNoTracking()
            .Include(c => c.City)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new RecentCustomerRow(
                c.AccountId,
                c.FullName,
                c.PrimaryConnectionType.DisplayName(),
                c.City!.Name,
                c.CreatedAt))
            .ToListAsync(ct);

        data.RecentConnections = await _db.Connections
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .OrderByDescending(c => c.ActivatedOn)
            .Take(5)
            .Select(c => new RecentConnectionRow(
                c.ConnectionNumber,
                c.Customer!.FullName,
                c.Customer.AccountId,
                c.Plan!.Name,
                c.City!.Name,
                c.Status.DisplayName(),
                c.Status.ToString(),
                c.IsBillable))
            .ToListAsync(ct);

        return Result<AdminDashboardData>.Ok(data);
    }
}

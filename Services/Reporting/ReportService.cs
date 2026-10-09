using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Orders;

namespace NEXUS.Services.Reporting;

/**
 * Management reporting. Every figure on the reports page comes from here, so
 * the page is a read of the live database rather than a fixture - which is the
 * whole point of a report.
 */
public interface IReportService
{
    Task<Result<ReportPage>> BuildAsync(ReportFilter filter, CancellationToken ct = default);
}

/** The reporting window and the optional narrowing every query shares. */
public sealed record ReportFilter(DateTime From, DateTime To, ConnectionType? Service)
{
    public static ReportFilter Default() => new(
        From: new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-5),
        To: DateTime.UtcNow.Date,
        Service: null);

    public string PeriodLabel => $"{From:dd MMM yyyy} - {To:dd MMM yyyy}";
    public string ServiceLabel => Service?.DisplayName() ?? "All services";
}

public sealed record ReportTotals(
    int Customers, int NewCustomers, int ActiveConnections, int TotalOrders,
    int CompletedOrders, decimal Revenue, decimal Outstanding, int OpenFeedback);

public sealed record MonthlyPoint(string Label, decimal Revenue, int Customers, int Orders);

/** One wedge or bar: a label and its weight. */
public sealed record Slice(string Label, decimal Value);

public sealed record CustomerReportRow(string AccountId, string Name, string City,
    string ConnectionType, string Plan, string Status, string StatusBadge,
    DateTime RegisteredOn, decimal MonthlyRate);

public sealed record OrderReportRow(string OrderId, string Customer, string ConnectionType,
    string Plan, string Status, string StatusBadge, DateTime PlacedOn, decimal Quoted);

public sealed record PaymentReportRow(string ReceiptNumber, string BillNumber, string Customer,
    decimal Amount, string Method, DateTime PaidOn);

public sealed class ReportPage
{
    public ReportFilter Filter { get; init; } = ReportFilter.Default();
    public ReportTotals Totals { get; init; } = new(0, 0, 0, 0, 0, 0m, 0m, 0);
    public IReadOnlyList<MonthlyPoint> Monthly { get; init; } = Array.Empty<MonthlyPoint>();
    public IReadOnlyList<Slice> ServiceMix { get; init; } = Array.Empty<Slice>();
    public IReadOnlyList<Slice> OrderPipeline { get; init; } = Array.Empty<Slice>();
    public IReadOnlyList<Slice> BillingMix { get; init; } = Array.Empty<Slice>();
    public IReadOnlyList<Slice> RevenueByService { get; init; } = Array.Empty<Slice>();
    public IReadOnlyList<CustomerReportRow> Customers { get; init; } = Array.Empty<CustomerReportRow>();
    public IReadOnlyList<OrderReportRow> Orders { get; init; } = Array.Empty<OrderReportRow>();
    public IReadOnlyList<PaymentReportRow> Payments { get; init; } = Array.Empty<PaymentReportRow>();
}

public sealed class ReportService : IReportService
{
    private const int RowCap = 200;

    private readonly NexusDbContext _db;

    public ReportService(NexusDbContext db) => _db = db;

    public async Task<Result<ReportPage>> BuildAsync(ReportFilter filter, CancellationToken ct = default)
    {
        if (filter.From > filter.To)
            return Result<ReportPage>.Fail(ErrorKind.Validation,
                "The start date is after the end date.");

        var from = filter.From.Date;
        // The end of the window is inclusive, so a report run on the 7th still
        // counts everything that happened during the 7th.
        var toExclusive = filter.To.Date.AddDays(1);

        var orders = _db.ConnectionOrders.AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt < toExclusive);

        var bills = _db.Bills.AsNoTracking()
            .Where(b => b.IssuedOn >= from && b.IssuedOn < toExclusive);

        var payments = _db.Payments.AsNoTracking()
            .Where(p => p.PaidOn >= from && p.PaidOn < toExclusive);

        var connections = _db.Connections.AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (filter.Service is ConnectionType type)
        {
            orders = orders.Where(o => o.ConnectionType == type);
            connections = connections.Where(c => c.ConnectionType == type);
            bills = bills.Where(b => b.Connection != null && b.Connection.ConnectionType == type);
            payments = payments.Where(p => p.Bill.Connection != null &&
                                           p.Bill.Connection.ConnectionType == type);
        }

        var customers = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted);

        var page = new ReportPage
        {
            Filter = filter,
            Totals = await TotalsAsync(from, toExclusive, orders, bills, payments, connections, ct),
            Monthly = await MonthlyAsync(from, toExclusive, orders, bills, customers, ct),
            ServiceMix = await ServiceMixAsync(connections, ct),
            OrderPipeline = await PipelineAsync(orders, ct),
            BillingMix = await BillingMixAsync(bills, ct),
            RevenueByService = await RevenueByServiceAsync(payments, ct),
            Customers = await CustomerRowsAsync(customers, filter, ct),
            Orders = await OrderRowsAsync(orders, ct),
            Payments = await PaymentRowsAsync(payments, ct)
        };

        return Result<ReportPage>.Ok(page);
    }

    private async Task<ReportTotals> TotalsAsync(
        DateTime from, DateTime toExclusive,
        IQueryable<ConnectionOrder> orders, IQueryable<Domain.Billing.Bill> bills,
        IQueryable<Domain.Billing.Payment> payments, IQueryable<Connection> connections,
        CancellationToken ct)
    {
        // Revenue is money actually received, not money invoiced. Outstanding
        // is the unpaid part of everything billed in the window.
        var revenue = await payments.SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var outstanding = await bills.SumAsync(b => (decimal?)(b.TotalAmount - b.AmountPaid), ct) ?? 0m;

        return new ReportTotals(
            Customers: await _db.Customers.AsNoTracking().CountAsync(c => !c.IsDeleted, ct),
            NewCustomers: await _db.Customers.AsNoTracking()
                .CountAsync(c => !c.IsDeleted && c.CreatedAt >= from && c.CreatedAt < toExclusive, ct),
            ActiveConnections: await connections.CountAsync(c => c.Status == ConnectionStatus.Active, ct),
            TotalOrders: await orders.CountAsync(ct),
            CompletedOrders: await orders.CountAsync(o => o.Status == OrderStatus.Completed, ct),
            Revenue: revenue,
            Outstanding: outstanding,
            OpenFeedback: await _db.Feedback.AsNoTracking().CountAsync(f => !f.IsResolved, ct));
    }

    /**
     * One row per calendar month in the window, including months with no
     * activity - a gap in a trend line is information, and dropping the month
     * would hide it.
     */
    private async Task<IReadOnlyList<MonthlyPoint>> MonthlyAsync(
        DateTime from, DateTime toExclusive,
        IQueryable<ConnectionOrder> orders, IQueryable<Domain.Billing.Bill> bills,
        IQueryable<Domain.Customers.Customer> customers, CancellationToken ct)
    {
        var billed = await bills
            .GroupBy(b => new { b.IssuedOn.Year, b.IssuedOn.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(b => b.TotalAmount) })
            .ToListAsync(ct);

        var placed = await orders
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var joined = await customers
            .Where(c => c.CreatedAt >= from && c.CreatedAt < toExclusive)
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var points = new List<MonthlyPoint>();
        for (var cursor = new DateTime(from.Year, from.Month, 1);
             cursor < toExclusive;
             cursor = cursor.AddMonths(1))
        {
            points.Add(new MonthlyPoint(
                cursor.ToString("MMM yy"),
                billed.FirstOrDefault(x => x.Year == cursor.Year && x.Month == cursor.Month)?.Amount ?? 0m,
                joined.FirstOrDefault(x => x.Year == cursor.Year && x.Month == cursor.Month)?.Count ?? 0,
                placed.FirstOrDefault(x => x.Year == cursor.Year && x.Month == cursor.Month)?.Count ?? 0));
        }

        return points;
    }

    private static async Task<IReadOnlyList<Slice>> ServiceMixAsync(
        IQueryable<Connection> connections, CancellationToken ct)
    {
        var rows = await connections
            .GroupBy(c => c.ConnectionType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows
            .OrderByDescending(r => r.Count)
            .Select(r => new Slice(r.Type.DisplayName(), r.Count))
            .ToList();
    }

    private static async Task<IReadOnlyList<Slice>> PipelineAsync(
        IQueryable<ConnectionOrder> orders, CancellationToken ct)
    {
        var rows = await orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows
            .OrderByDescending(r => r.Count)
            .Select(r => new Slice(r.Status.DisplayName(), r.Count))
            .ToList();
    }

    private static async Task<IReadOnlyList<Slice>> BillingMixAsync(
        IQueryable<Domain.Billing.Bill> bills, CancellationToken ct)
    {
        var rows = await bills
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows
            .OrderByDescending(r => r.Count)
            .Select(r => new Slice(r.Status.DisplayName(), r.Count))
            .ToList();
    }

    private static async Task<IReadOnlyList<Slice>> RevenueByServiceAsync(
        IQueryable<Domain.Billing.Payment> payments, CancellationToken ct)
    {
        var rows = await payments
            .Where(p => p.Bill.Connection != null)
            .GroupBy(p => p.Bill.Connection!.ConnectionType)
            .Select(g => new { Type = g.Key, Amount = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        return rows
            .OrderByDescending(r => r.Amount)
            .Select(r => new Slice(r.Type.DisplayName(), r.Amount))
            .ToList();
    }

    private static async Task<IReadOnlyList<CustomerReportRow>> CustomerRowsAsync(
        IQueryable<Domain.Customers.Customer> customers, ReportFilter filter, CancellationToken ct)
    {
        IQueryable<Domain.Customers.Customer> query = customers.Include(c => c.City);

        if (filter.Service is ConnectionType type)
            query = query.Where(c => c.PrimaryConnectionType == type);

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(RowCap)
            .Select(c => new
            {
                c.AccountId,
                c.FullName,
                City = c.City != null ? c.City.Name : string.Empty,
                c.PrimaryConnectionType,
                c.CreatedAt,
                Connection = c.Connections
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.Status == ConnectionStatus.Active)
                    .ThenByDescending(x => x.ActivatedOn)
                    .Select(x => new
                    {
                        x.Status,
                        PlanName = x.Plan.Name,
                        // The monthly cycle is the comparable figure across
                        // plans; a yearly-only plan still has an equivalent.
                        Rate = x.Plan.Prices
                            .Where(p => p.Cycle == BillingCycle.Monthly && p.IsAvailable)
                            .Select(p => (decimal?)p.Amount)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Select(r => new CustomerReportRow(
            r.AccountId,
            r.FullName,
            r.City,
            r.PrimaryConnectionType.DisplayName(),
            r.Connection?.PlanName ?? "No connection",
            r.Connection is null ? "Pending" : r.Connection.Status.DisplayName(),
            r.Connection is null ? "badge-neutral" : StatusBadge(r.Connection.Status),
            r.CreatedAt,
            r.Connection?.Rate ?? 0m)).ToList();
    }

    private static string StatusBadge(ConnectionStatus status) => status switch
    {
        ConnectionStatus.Active => "badge-success",
        ConnectionStatus.Pending => "badge-warning",
        ConnectionStatus.TemporarilyInactive => "badge-warning",
        _ => "badge-danger"
    };

    private static async Task<IReadOnlyList<OrderReportRow>> OrderRowsAsync(
        IQueryable<ConnectionOrder> orders, CancellationToken ct)
    {
        var rows = await orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(RowCap)
            .Select(o => new
            {
                o.OrderId,
                Customer = o.Customer.FullName,
                o.ConnectionType,
                Plan = o.Plan.Name,
                o.Status,
                o.CreatedAt,
                o.QuotedAmount
            })
            .ToListAsync(ct);

        return rows.Select(o => new OrderReportRow(
            o.OrderId, o.Customer, o.ConnectionType.DisplayName(), o.Plan,
            o.Status.DisplayName(), o.Status.BadgeClass(), o.CreatedAt, o.QuotedAmount)).ToList();
    }

    private static async Task<IReadOnlyList<PaymentReportRow>> PaymentRowsAsync(
        IQueryable<Domain.Billing.Payment> payments, CancellationToken ct)
    {
        var rows = await payments
            .OrderByDescending(p => p.PaidOn)
            .Take(RowCap)
            .Select(p => new
            {
                p.ReceiptNumber,
                BillNumber = p.Bill.BillNumber,
                Customer = p.Customer.FullName,
                p.Amount,
                p.Method,
                p.PaidOn
            })
            .ToListAsync(ct);

        return rows.Select(p => new PaymentReportRow(
            p.ReceiptNumber, p.BillNumber, p.Customer, p.Amount,
            p.Method.DisplayName(), p.PaidOn)).ToList();
    }
}

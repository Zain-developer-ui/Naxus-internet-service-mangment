using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Data;

namespace NEXUS.Services.Orders;

/**
 * The full order register for staff.
 *
 * The survey queue shows work still in flight; this is every order the system
 * has ever taken, including the closed and cancelled ones. Accounts and admin
 * need that wider view to answer "what happened to order X" without walking
 * the queue.
 */
public interface IOrderRegisterService
{
    Task<Result<OrderRegisterPage>> ListAsync(OrderStatus? status, ConnectionType? type,
                                              string? search, CancellationToken ct = default);
}

public sealed record OrderRegisterRow(int Id, string OrderId, string Customer, string AccountId,
    string ConnectionType, string Plan, string City, string Status, string StatusBadge,
    DateTime PlacedOn, DateTime? ScheduledFor, decimal Quoted);

public sealed record OrderRegisterTotals(int Total, int Open, int Completed, int Cancelled);

public sealed class OrderRegisterPage
{
    public IReadOnlyList<OrderRegisterRow> Orders { get; init; } = Array.Empty<OrderRegisterRow>();
    public OrderRegisterTotals Totals { get; init; } = new(0, 0, 0, 0);
    public OrderStatus? Status { get; init; }
    public ConnectionType? Type { get; init; }
    public string? Search { get; init; }
}

public sealed class OrderRegisterService : IOrderRegisterService
{
    private const int RowCap = 300;

    private readonly NexusDbContext _db;

    public OrderRegisterService(NexusDbContext db) => _db = db;

    public async Task<Result<OrderRegisterPage>> ListAsync(OrderStatus? status, ConnectionType? type,
                                                           string? search, CancellationToken ct = default)
    {
        var query = _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.City)
            .AsQueryable();

        if (status is OrderStatus s)
            query = query.Where(o => o.Status == s);

        if (type is ConnectionType t)
            query = query.Where(o => o.ConnectionType == t);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.OrderId, $"%{term}%") ||
                EF.Functions.Like(o.Customer.FullName, $"%{term}%") ||
                EF.Functions.Like(o.Customer.AccountId, $"%{term}%"));
        }

        var rows = await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(RowCap)
            .Select(o => new OrderRegisterRow(
                o.Id,
                o.OrderId,
                o.Customer.FullName,
                o.Customer.AccountId,
                o.ConnectionType.DisplayName(),
                o.Plan.Name,
                o.City.Name,
                o.Status.DisplayName(),
                o.Status.BadgeClass(),
                o.CreatedAt,
                o.ScheduledFor,
                o.QuotedAmount))
            .ToListAsync(ct);

        // Totals cover the whole register, not the filtered slice - the header
        // cards are a health read, not a count of what is on this page.
        var all = _db.ConnectionOrders.AsNoTracking();

        var totals = new OrderRegisterTotals(
            Total: await all.CountAsync(ct),
            Open: await all.CountAsync(o => o.Status != OrderStatus.Completed &&
                                            o.Status != OrderStatus.Cancelled, ct),
            Completed: await all.CountAsync(o => o.Status == OrderStatus.Completed, ct),
            Cancelled: await all.CountAsync(o => o.Status == OrderStatus.Cancelled, ct));

        return Result<OrderRegisterPage>.Ok(new OrderRegisterPage
        {
            Orders = rows,
            Totals = totals,
            Status = status,
            Type = type,
            Search = search
        });
    }
}

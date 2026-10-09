using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Search;

public interface IAdvancedSearchService
{
    Task<Result<AdvancedSearchResults>> SearchAsync(AdvancedSearchQuery query, CancellationToken ct = default);
}

/**
 * The advanced search the SRS asks for: the unique id, the name, the connection
 * type, a date or period, and the contact number.
 *
 * Orders are filtered on the date they were placed and connections on the date
 * they were activated, which is what "date or period of application or received
 * the connection" means - one period, read against whichever date the row owns.
 */
public sealed class AdvancedSearchService : IAdvancedSearchService
{
    private const int RowCap = 100;

    private readonly NexusDbContext _db;

    public AdvancedSearchService(NexusDbContext db) => _db = db;

    public async Task<Result<AdvancedSearchResults>> SearchAsync(
        AdvancedSearchQuery query, CancellationToken ct = default)
    {
        if (!query.HasAnyFilter)
            return Result<AdvancedSearchResults>.Ok(new AdvancedSearchResults { Query = query });

        // An end date means the whole of that day, not midnight at its start.
        var from = query.From?.Date;
        var to = query.To?.Date.AddDays(1);

        var orders = await BuildOrdersAsync(query, from, to, ct);
        var connections = await BuildConnectionsAsync(query, from, to, ct);

        return Result<AdvancedSearchResults>.Ok(new AdvancedSearchResults
        {
            Query = query,
            Orders = orders,
            Connections = connections,
            Searched = true
        });
    }

    private async Task<IReadOnlyList<SearchOrderRow>> BuildOrdersAsync(
        AdvancedSearchQuery query, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Id))
        {
            var like = Like(query.Id);
            q = q.Where(o => EF.Functions.Like(o.OrderId, like));
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var like = Like(query.Name);
            q = q.Where(o => EF.Functions.Like(o.Customer!.FullName, like));
        }

        if (query.Type is ConnectionType type)
            q = q.Where(o => o.ConnectionType == type);

        if (from is not null)
            q = q.Where(o => o.CreatedAt >= from);

        if (to is not null)
            q = q.Where(o => o.CreatedAt < to);

        if (!string.IsNullOrWhiteSpace(query.Contact))
        {
            var like = Like(query.Contact);
            q = q.Where(o => EF.Functions.Like(o.Customer!.Phone, like));
        }

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
        AdvancedSearchQuery query, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = _db.Connections
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Id))
        {
            var like = Like(query.Id);
            q = q.Where(c => EF.Functions.Like(c.ConnectionNumber, like) ||
                             EF.Functions.Like(c.Customer!.AccountId, like));
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var like = Like(query.Name);
            q = q.Where(c => EF.Functions.Like(c.Customer!.FullName, like));
        }

        if (query.Type is ConnectionType type)
            q = q.Where(c => c.ConnectionType == type);

        if (from is not null)
            q = q.Where(c => c.ActivatedOn >= from);

        if (to is not null)
            q = q.Where(c => c.ActivatedOn < to);

        if (!string.IsNullOrWhiteSpace(query.Contact))
        {
            var like = Like(query.Contact);
            q = q.Where(c => EF.Functions.Like(c.Customer!.Phone, like));
        }

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

    private static string Like(string term) => $"%{term.Trim()}%";
}

using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Domain.Billing;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Search;

public interface ISearchService
{
    Task<Result<SearchPageData>> SearchAsync(string? term, CancellationToken ct = default);
}

/**
 * Single entry point behind the retail search screen. An empty term returns an
 * empty page rather than everything - a clerk landing on this screen has not
 * asked for the whole customer book.
 */
public sealed class SearchService : ISearchService
{
    private const int LimitPerSet = 25;

    private readonly NexusDbContext _db;

    public SearchService(NexusDbContext db) => _db = db;

    public async Task<Result<SearchPageData>> SearchAsync(string? term, CancellationToken ct = default)
    {
        term = term?.Trim();
        if (string.IsNullOrWhiteSpace(term))
            return Result<SearchPageData>.Ok(new SearchPageData { Term = term });

        var like = $"%{term}%";

        var page = new SearchPageData { Term = term };

        page.Customers = await _db.Customers
            .AsNoTracking()
            .Include(c => c.City)
            .Where(c => !c.IsDeleted &&
                        (EF.Functions.Like(c.FullName, like) ||
                         EF.Functions.Like(c.AccountId, like) ||
                         EF.Functions.Like(c.Phone, like) ||
                         EF.Functions.Like(c.Cnic, like)))
            .OrderBy(c => c.FullName)
            .Take(LimitPerSet)
            .Select(c => new SearchCustomerRow(
                c.Id,
                c.AccountId,
                c.FullName,
                c.Phone,
                c.PrimaryConnectionType.DisplayName(),
                c.Connections
                    .Where(x => !x.IsDeleted)
                    .Select(x => x.Plan.Name)
                    .FirstOrDefault() ?? "No plan",
                c.City!.Name,
                c.Connections
                    .Where(x => !x.IsDeleted)
                    .Select(x => x.Status)
                    .FirstOrDefault()
                    .DisplayName(),
                c.Connections
                    .Where(x => !x.IsDeleted)
                    .Select(x => x.Status)
                    .FirstOrDefault()
                    .ToString()))
            .ToListAsync(ct);

        page.Employees = await _db.Employees
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Shop)
            .Where(e => EF.Functions.Like(e.FullName, like) ||
                        EF.Functions.Like(e.EmployeeCode, like) ||
                        EF.Functions.Like(e.Designation, like))
            .OrderBy(e => e.FullName)
            .Take(LimitPerSet)
            .Select(e => new SearchEmployeeRow(
                e.EmployeeCode,
                e.FullName,
                e.User.Role,
                e.Designation,
                e.Shop != null ? e.Shop.Name : "Head Office",
                e.IsActive ? "Active" : "Inactive",
                e.IsActive ? "Active" : "TemporarilyInactive"))
            .ToListAsync(ct);

        page.Orders = await _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Where(o => EF.Functions.Like(o.OrderId, like) ||
                        EF.Functions.Like(o.Customer!.FullName, like))
            .OrderByDescending(o => o.CreatedAt)
            .Take(LimitPerSet)
            .Select(o => new SearchOrderRow(
                o.OrderId,
                o.Customer!.FullName,
                o.ConnectionType.DisplayName(),
                o.Plan!.Name,
                o.Status.DisplayName(),
                o.Status.ToString(),
                o.CreatedAt))
            .ToListAsync(ct);

        page.Accounts = await _db.Connections
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Where(c => !c.IsDeleted &&
                        (EF.Functions.Like(c.Customer!.AccountId, like) ||
                         EF.Functions.Like(c.Customer.FullName, like) ||
                         EF.Functions.Like(c.ConnectionNumber, like)))
            .OrderBy(c => c.Customer!.FullName)
            .Take(LimitPerSet)
            .Select(c => new SearchAccountRow(
                c.Customer!.AccountId,
                c.Customer.FullName,
                c.ConnectionType.DisplayName(),
                c.Plan!.Name,
                c.Bills
                    .OrderByDescending(b => b.IssuedOn)
                    .Select(b => b.Status)
                    .FirstOrDefault()
                    .DisplayName(),
                c.Bills
                    .OrderByDescending(b => b.IssuedOn)
                    .Select(b => b.Status)
                    .FirstOrDefault()
                    .ToString(),
                c.Status.DisplayName(),
                c.Status.ToString()))
            .ToListAsync(ct);

        return Result<SearchPageData>.Ok(page);
    }
}

using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Domain.Catalog;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Catalog;

/**
 * Write-side of the plan catalogue, used by the admin console. The public
 * PlanCatalogService stays read-only so a marketing page can never mutate a
 * plan by accident.
 */
public interface IPlanAdminService
{
    Task<Result<PlanListPage>> ListAsync(ConnectionType? type, string? search,
                                         bool showInactive, CancellationToken ct = default);
    Task<Result<PlanEditViewModel>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(PlanEditViewModel model, CancellationToken ct = default);
    Task<Result<int>> UpdateAsync(PlanEditViewModel model, CancellationToken ct = default);
    Task<Result<bool>> SetActiveAsync(int id, bool active, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class PlanAdminService : IPlanAdminService
{
    private readonly NexusDbContext _db;

    public PlanAdminService(NexusDbContext db) => _db = db;

    public async Task<Result<PlanListPage>> ListAsync(ConnectionType? type, string? search,
                                                      bool showInactive, CancellationToken ct = default)
    {
        var query = _db.Plans.AsNoTracking().AsQueryable();

        // Inactive plans are hidden by default so the working list is the one
        // that customers can actually order. The toggle is what brings the
        // retired ones back for editing.
        if (!showInactive)
            query = query.Where(p => p.IsActive);

        if (type is not null)
            query = query.Where(p => p.ConnectionType == type);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{term}%") ||
                EF.Functions.Like(p.Code, $"%{term}%"));
        }

        var rows = await query
            .OrderBy(p => p.ConnectionType)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new PlanRow(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.ConnectionType.DisplayName(),
                p.ConnectionType.ToString(),
                p.IsUnlimited,
                p.HoursIncluded,
                p.SpeedKbps,
                p.SecurityDeposit,
                p.Prices
                    .Where(x => x.Cycle == BillingCycle.Monthly && x.IsAvailable)
                    .Select(x => (decimal?)x.Amount)
                    .FirstOrDefault(),
                p.SortOrder,
                p.IsActive,
                p.Orders.Count))
            .ToListAsync(ct);

        // Totals describe the whole catalogue, not the filtered slice - the
        // header cards are a health read on the estate, not on this page.
        var all = _db.Plans.AsNoTracking();
        var totals = new PlanRegisterTotals(
            await all.CountAsync(ct),
            await all.CountAsync(p => p.IsActive, ct),
            await all.CountAsync(p => !p.IsActive, ct),
            await all.CountAsync(p => p.IsUnlimited, ct));

        return Result<PlanListPage>.Ok(new PlanListPage
        {
            Plans = rows,
            Filter = type,
            Search = search,
            ShowInactive = showInactive,
            Totals = totals
        });
    }

    public async Task<Result<PlanEditViewModel>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .AsNoTracking()
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null) return Result<PlanEditViewModel>.NotFound("Plan");

        var model = new PlanEditViewModel
        {
            Id = plan.Id,
            Code = plan.Code,
            Name = plan.Name,
            ConnectionType = plan.ConnectionType,
            IsUnlimited = plan.IsUnlimited,
            HoursIncluded = plan.HoursIncluded,
            SpeedKbps = plan.SpeedKbps,
            Description = plan.Description,
            IsActive = plan.IsActive,
            SortOrder = plan.SortOrder,
            SecurityDeposit = plan.SecurityDeposit,
            Prices = BuildPriceRows(plan.Prices)
        };

        return Result<PlanEditViewModel>.Ok(model);
    }

    public async Task<Result<int>> CreateAsync(PlanEditViewModel model, CancellationToken ct = default)
    {
        var code = model.Code.Trim();

        if (await _db.Plans.AnyAsync(p => p.Code == code, ct))
            return Result<int>.Fail(nameof(model.Code), $"Plan code '{code}' is already in use.");

        var plan = new Plan
        {
            Code = code,
            Name = model.Name.Trim(),
            ConnectionType = model.ConnectionType,
            IsUnlimited = model.IsUnlimited,
            HoursIncluded = model.IsUnlimited ? null : model.HoursIncluded,
            SpeedKbps = model.SpeedKbps,
            Description = model.Description?.Trim() ?? string.Empty,
            IsActive = model.IsActive,
            SortOrder = model.SortOrder,
            SecurityDeposit = model.SecurityDeposit
        };

        ApplyPrices(plan, model);

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);

        return Result<int>.Ok(plan.Id);
    }

    public async Task<Result<int>> UpdateAsync(PlanEditViewModel model, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == model.Id, ct);

        if (plan is null) return Result<int>.NotFound("Plan");

        var code = model.Code.Trim();

        if (await _db.Plans.AnyAsync(p => p.Code == code && p.Id != model.Id, ct))
            return Result<int>.Fail(nameof(model.Code), $"Plan code '{code}' is already in use.");

        plan.Code = code;
        plan.Name = model.Name.Trim();
        plan.ConnectionType = model.ConnectionType;
        plan.IsUnlimited = model.IsUnlimited;
        plan.HoursIncluded = model.IsUnlimited ? null : model.HoursIncluded;
        plan.SpeedKbps = model.SpeedKbps;
        plan.Description = model.Description?.Trim() ?? string.Empty;
        plan.IsActive = model.IsActive;
        plan.SortOrder = model.SortOrder;
        plan.SecurityDeposit = model.SecurityDeposit;

        ApplyPrices(plan, model);

        await _db.SaveChangesAsync(ct);

        return Result<int>.Ok(plan.Id);
    }

    public async Task<Result<bool>> SetActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return Result<bool>.NotFound("Plan");

        plan.IsActive = active;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(active);
    }

    /**
     * A plan that has ever been ordered is retired, never removed - the order
     * history and every bill raised from it point back here. Deletion is only
     * offered for a plan nothing has touched yet.
     */
    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null) return Result<bool>.NotFound("Plan");

        var orderCount = await _db.ConnectionOrders.CountAsync(o => o.PlanId == id, ct);
        if (orderCount > 0)
            return Result<bool>.Conflict(
                $"'{plan.Name}' has {orderCount} order(s) against it. Set it inactive instead of deleting.");

        _db.PlanPrices.RemoveRange(plan.Prices);
        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    private static List<PlanPriceRow> BuildPriceRows(IEnumerable<PlanPrice> prices)
    {
        var byCycle = prices.ToDictionary(p => p.Cycle);
        var rows = new List<PlanPriceRow>();

        foreach (var cycle in new[] { BillingCycle.Monthly, BillingCycle.HalfYearly, BillingCycle.Yearly })
        {
            if (byCycle.TryGetValue(cycle, out var existing))
                rows.Add(new PlanPriceRow
                {
                    Cycle = cycle,
                    Amount = existing.Amount,
                    IsAvailable = existing.IsAvailable
                });
            else
                rows.Add(new PlanPriceRow { Cycle = cycle, Amount = 0m, IsAvailable = false });
        }

        return rows;
    }

    /**
     * Prices are replaced in place rather than cleared and re-added, so a row
     * an operator left switched off keeps its id and does not churn.
     */
    private static void ApplyPrices(Plan plan, PlanEditViewModel model)
    {
        foreach (var row in model.Prices)
        {
            var existing = plan.Prices.FirstOrDefault(p => p.Cycle == row.Cycle);

            if (existing is null)
            {
                plan.Prices.Add(new PlanPrice
                {
                    Cycle = row.Cycle,
                    Amount = row.Amount,
                    IsAvailable = row.IsAvailable
                });
                continue;
            }

            existing.Amount = row.Amount;
            existing.IsAvailable = row.IsAvailable;
        }
    }
}

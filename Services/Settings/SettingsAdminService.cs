using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Data;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Organisation;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Settings;

/**
 * Cities and bulk discount slabs. Both are edited rarely and read on every
 * registration and every bill, so the read side is deliberately plain.
 */
public interface ISettingsAdminService
{
    Task<Result<CityListPage>> ListCitiesAsync(string? search, CancellationToken ct = default);
    Task<Result<CityEditViewModel>> GetCityAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveCityAsync(CityEditViewModel model, CancellationToken ct = default);
    Task<Result<bool>> ToggleCityServedAsync(int id, bool served, CancellationToken ct = default);
    Task<Result<bool>> DeleteCityAsync(int id, CancellationToken ct = default);

    Task<Result<DiscountTierListPage>> ListTiersAsync(CancellationToken ct = default);
    Task<Result<DiscountTierEditViewModel>> GetTierAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveTierAsync(DiscountTierEditViewModel model, CancellationToken ct = default);
    Task<Result<bool>> ToggleTierAsync(int id, bool active, CancellationToken ct = default);
    Task<Result<bool>> DeleteTierAsync(int id, CancellationToken ct = default);
}

public sealed class SettingsAdminService : ISettingsAdminService
{
    private readonly NexusDbContext _db;

    public SettingsAdminService(NexusDbContext db) => _db = db;

    // ---------------------------------------------------------------- cities

    public async Task<Result<CityListPage>> ListCitiesAsync(string? search, CancellationToken ct = default)
    {
        IQueryable<City> query = _db.Cities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                EF.Functions.Like(c.Name, $"%{term}%") ||
                EF.Functions.Like(c.Region, $"%{term}%"));
        }

        var rows = await query
            .OrderBy(c => c.Name)
            .Select(c => new CityRow(
                c.Id,
                c.Name,
                c.Region,
                c.Code,
                c.IsServiced,
                c.Customers.Count,
                c.Shops.Count))
            .ToListAsync(ct);

        var all = _db.Cities.AsNoTracking();
        var totals = new CityRegisterTotals(
            await all.CountAsync(ct),
            await all.CountAsync(c => c.IsServiced, ct),
            await _db.Customers.CountAsync(ct));

        return Result<CityListPage>.Ok(new CityListPage
        {
            Cities = rows,
            Search = search,
            Totals = totals
        });
    }

    public async Task<Result<CityEditViewModel>> GetCityAsync(int id, CancellationToken ct = default)
    {
        var city = await _db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (city is null) return Result<CityEditViewModel>.NotFound("City");

        return Result<CityEditViewModel>.Ok(new CityEditViewModel
        {
            Id = city.Id,
            Name = city.Name,
            Region = city.Region,
            Code = city.Code,
            IsServiced = city.IsServiced,
            CustomerCount = await _db.Customers.CountAsync(c => c.CityId == id, ct)
        });
    }

    public async Task<Result<int>> SaveCityAsync(CityEditViewModel model, CancellationToken ct = default)
    {
        var name = model.Name.Trim();

        if (await _db.Cities.AnyAsync(c => c.Name == name && c.Id != model.Id, ct))
            return Result<int>.Fail(nameof(model.Name), $"A city named '{name}' already exists.");

        if (await _db.Cities.AnyAsync(c => c.Code == model.Code && c.Id != model.Id, ct))
            return Result<int>.Fail(nameof(model.Code),
                $"City code {model.Code} is already assigned to another city.");

        if (model.Id == 0)
        {
            var city = new City
            {
                Name = name,
                Region = model.Region?.Trim() ?? string.Empty,
                Code = model.Code,
                IsServiced = model.IsServiced
            };

            _db.Cities.Add(city);
            await _db.SaveChangesAsync(ct);
            return Result<int>.Ok(city.Id);
        }

        var existing = await _db.Cities.FirstOrDefaultAsync(c => c.Id == model.Id, ct);
        if (existing is null) return Result<int>.NotFound("City");

        // The code is baked into account IDs already issued from this city, so
        // it is frozen the moment one exists. Renaming stays allowed.
        var inUse = await _db.Customers.AnyAsync(c => c.CityId == model.Id, ct);
        if (inUse && existing.Code != model.Code)
            return Result<int>.Fail(nameof(model.Code),
                "This city code is embedded in existing account IDs. It cannot change while customers are registered here.");

        existing.Name = name;
        existing.Region = model.Region?.Trim() ?? string.Empty;
        existing.Code = model.Code;
        existing.IsServiced = model.IsServiced;

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(existing.Id);
    }

    public async Task<Result<bool>> ToggleCityServedAsync(int id, bool served, CancellationToken ct = default)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (city is null) return Result<bool>.NotFound("City");

        city.IsServiced = served;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(served);
    }

    public async Task<Result<bool>> DeleteCityAsync(int id, CancellationToken ct = default)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (city is null) return Result<bool>.NotFound("City");

        if (await _db.Customers.AnyAsync(c => c.CityId == id, ct))
            return Result<bool>.Conflict(
                $"'{city.Name}' has customers registered against it. Mark it not served instead of deleting.");

        if (await _db.RetailShops.AnyAsync(s => s.CityId == id, ct))
            return Result<bool>.Conflict(
                $"'{city.Name}' still has retail outlets. Reassign or remove them first.");

        _db.Cities.Remove(city);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    // -------------------------------------------------------- discount tiers

    public async Task<Result<DiscountTierListPage>> ListTiersAsync(CancellationToken ct = default)
    {
        var tiers = await _db.BulkDiscountTiers
            .AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.MinConnections)
            .ToListAsync(ct);

        // Two slabs covering the same headcount means one of them never fires.
        // Flagging it in the list is cheaper than discovering it on a bill.
        var rows = tiers.Select(t => new DiscountTierRow(
            t.Id,
            t.MinConnections,
            t.MaxConnections,
            t.Label,
            t.PercentRate,
            t.SortOrder,
            t.IsActive,
            tiers.Any(o => o.Id != t.Id && o.IsActive && t.IsActive &&
                           o.MinConnections < (t.MaxConnections ?? int.MaxValue) &&
                           t.MinConnections < (o.MaxConnections ?? int.MaxValue))))
            .ToList();

        return Result<DiscountTierListPage>.Ok(new DiscountTierListPage
        {
            Tiers = rows,
            ActiveCount = rows.Count(r => r.IsActive),
            InactiveCount = rows.Count(r => !r.IsActive)
        });
    }

    public async Task<Result<DiscountTierEditViewModel>> GetTierAsync(int id, CancellationToken ct = default)
    {
        var tier = await _db.BulkDiscountTiers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tier is null) return Result<DiscountTierEditViewModel>.NotFound("Discount tier");

        return Result<DiscountTierEditViewModel>.Ok(new DiscountTierEditViewModel
        {
            Id = tier.Id,
            MinConnections = tier.MinConnections,
            MaxConnections = tier.MaxConnections,
            IsOpenEnded = tier.MaxConnections is null,
            Rate = tier.Rate,
            IsActive = tier.IsActive,
            SortOrder = tier.SortOrder
        });
    }

    public async Task<Result<int>> SaveTierAsync(DiscountTierEditViewModel model, CancellationToken ct = default)
    {
        var max = model.IsOpenEnded ? (int?)null : model.MaxConnections;

        if (!model.IsOpenEnded && max is null)
            return Result<int>.Fail(nameof(model.MaxConnections),
                "Enter an upper bound, or tick open ended for the top slab.");

        if (max is not null && max <= model.MinConnections)
            return Result<int>.Fail(nameof(model.MaxConnections),
                "Upper bound must be greater than the minimum.");

        // Overlap check across the other rows. Two active slabs covering the
        // same headcount make the discount applied on a bill ambiguous.
        var upper = max ?? int.MaxValue;
        var clash = await _db.BulkDiscountTiers
            .Where(t => t.Id != model.Id && t.IsActive && model.IsActive)
            .Where(t => t.MinConnections < upper &&
                        model.MinConnections < (t.MaxConnections ?? int.MaxValue))
            .Select(t => new { t.MinConnections, t.MaxConnections })
            .FirstOrDefaultAsync(ct);

        if (clash is not null)
        {
            var clashLabel = clash.MaxConnections is null
                ? $"{clash.MinConnections}+"
                : $"{clash.MinConnections}-{clash.MaxConnections}";
            return Result<int>.Fail(nameof(model.MinConnections),
                $"This slab overlaps the active {clashLabel} slab. Adjust the bounds or deactivate one.");
        }

        if (model.Id == 0)
        {
            var tier = new BulkDiscountTierEntity
            {
                MinConnections = model.MinConnections,
                MaxConnections = max,
                Rate = model.Rate,
                IsActive = model.IsActive,
                SortOrder = model.SortOrder
            };

            _db.BulkDiscountTiers.Add(tier);
            await _db.SaveChangesAsync(ct);
            return Result<int>.Ok(tier.Id);
        }

        var existing = await _db.BulkDiscountTiers.FirstOrDefaultAsync(t => t.Id == model.Id, ct);
        if (existing is null) return Result<int>.NotFound("Discount tier");

        existing.MinConnections = model.MinConnections;
        existing.MaxConnections = max;
        existing.Rate = model.Rate;
        existing.IsActive = model.IsActive;
        existing.SortOrder = model.SortOrder;

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(existing.Id);
    }

    public async Task<Result<bool>> ToggleTierAsync(int id, bool active, CancellationToken ct = default)
    {
        var tier = await _db.BulkDiscountTiers.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tier is null) return Result<bool>.NotFound("Discount tier");

        tier.IsActive = active;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(active);
    }

    public async Task<Result<bool>> DeleteTierAsync(int id, CancellationToken ct = default)
    {
        var tier = await _db.BulkDiscountTiers.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tier is null) return Result<bool>.NotFound("Discount tier");

        _db.BulkDiscountTiers.Remove(tier);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }
}

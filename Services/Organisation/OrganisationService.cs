using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Organisation;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Organisation;

/**
 * Outlets and the staff who run them. An employee record is always tied to a
 * login - the technical portal filters jobs by the employee behind the signed
 * in user, so a floating staff row would be invisible to the system.
 */
public interface IOrganisationService
{
    Task<Result<OrgListPage>> ListAsync(CancellationToken ct = default);

    Task<Result<ShopEditViewModel>> GetShopAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveShopAsync(ShopEditViewModel model, CancellationToken ct = default);
    Task<Result<bool>> SetShopActiveAsync(int id, bool active, CancellationToken ct = default);
    Task<Result<bool>> DeleteShopAsync(int id, CancellationToken ct = default);

    Task<Result<OrgLists>> GetListsAsync(CancellationToken ct = default);
    Task<Result<EmployeeRow>> GetEmployeeAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveEmployeeAsync(EmployeeEditViewModel model, CancellationToken ct = default);
    Task<Result<bool>> SetEmployeeActiveAsync(int id, bool active, CancellationToken ct = default);

    Task<IReadOnlyList<CityOption>> CityOptionsAsync(CancellationToken ct = default);
}

public sealed class OrganisationService : IOrganisationService
{
    private readonly NexusDbContext _db;

    public OrganisationService(NexusDbContext db) => _db = db;

    public async Task<Result<OrgListPage>> ListAsync(CancellationToken ct = default)
    {
        var shops = await _db.RetailShops
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ShopRow(
                s.Id,
                s.Name,
                s.OutletCode,
                s.City.Name,
                s.Address,
                s.Phone,
                s.IsActive,
                s.Staff.Count(),
                s.Stock.Count()))
            .ToListAsync(ct);

        var employees = await _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeRow(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Designation,
                e.Phone,
                e.Email ?? string.Empty,
                e.Shop != null ? e.Shop.Name : null,
                e.User.Role,
                e.JoinedOn.ToString("dd MMM yyyy"),
                e.IsActive,
                e.ShopId,
                e.UserId,
                e.JoinedOn))
            .ToListAsync(ct);

        var options = await _db.RetailShops
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ShopOption(s.Id, s.Name, s.OutletCode))
            .ToListAsync(ct);

        return Result<OrgListPage>.Ok(new OrgListPage
        {
            Shops = shops,
            Employees = employees,
            ShopOptions = options,
            Totals = new OrgTotals(
                shops.Count,
                shops.Count(s => s.IsActive),
                employees.Count,
                employees.Count(e => e.IsActive))
        });
    }

    // ---------------------------------------------------------------- shops

    public async Task<Result<ShopEditViewModel>> GetShopAsync(int id, CancellationToken ct = default)
    {
        var shop = await _db.RetailShops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shop is null) return Result<ShopEditViewModel>.NotFound("Outlet");

        return Result<ShopEditViewModel>.Ok(new ShopEditViewModel
        {
            Id = shop.Id,
            Name = shop.Name,
            OutletCode = shop.OutletCode,
            Address = shop.Address,
            Phone = shop.Phone,
            CityId = shop.CityId,
            IsActive = shop.IsActive,
            Cities = await CityOptionsAsync(ct),
            StaffCount = await _db.Employees.CountAsync(e => e.ShopId == id, ct)
        });
    }

    public static ShopEditViewModel BlankShop(IReadOnlyList<CityOption> cities) => new()
    {
        Cities = cities
    };

    public async Task<Result<int>> SaveShopAsync(ShopEditViewModel model, CancellationToken ct = default)
    {
        var code = model.OutletCode.Trim().ToUpperInvariant();

        if (await _db.RetailShops.AnyAsync(s => s.OutletCode == code && s.Id != model.Id, ct))
            return Result<int>.Fail(nameof(model.OutletCode), $"Outlet code '{code}' is already in use.");

        if (!await _db.Cities.AnyAsync(c => c.Id == model.CityId, ct))
            return Result<int>.Fail(nameof(model.CityId), "Pick a city that exists.");

        if (model.Id == 0)
        {
            var shop = new RetailShop
            {
                Name = model.Name.Trim(),
                OutletCode = code,
                Address = model.Address?.Trim() ?? string.Empty,
                Phone = model.Phone?.Trim() ?? string.Empty,
                CityId = model.CityId,
                IsActive = model.IsActive
            };

            _db.RetailShops.Add(shop);
            await _db.SaveChangesAsync(ct);
            return Result<int>.Ok(shop.Id);
        }

        var existing = await _db.RetailShops.FirstOrDefaultAsync(s => s.Id == model.Id, ct);
        if (existing is null) return Result<int>.NotFound("Outlet");

        existing.Name = model.Name.Trim();
        existing.OutletCode = code;
        existing.Address = model.Address?.Trim() ?? string.Empty;
        existing.Phone = model.Phone?.Trim() ?? string.Empty;
        existing.CityId = model.CityId;
        existing.IsActive = model.IsActive;

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(existing.Id);
    }

    public async Task<Result<bool>> SetShopActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var shop = await _db.RetailShops.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shop is null) return Result<bool>.NotFound("Outlet");

        shop.IsActive = active;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(active);
    }

    public async Task<Result<bool>> DeleteShopAsync(int id, CancellationToken ct = default)
    {
        var shop = await _db.RetailShops.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shop is null) return Result<bool>.NotFound("Outlet");

        if (await _db.Employees.AnyAsync(e => e.ShopId == id, ct))
            return Result<bool>.Conflict($"'{shop.Name}' still has staff assigned. Move them first.");

        if (await _db.StockItems.AnyAsync(s => s.ShopId == id && s.Quantity > 0, ct))
            return Result<bool>.Conflict($"'{shop.Name}' still holds stock. Transfer it before deleting.");

        var emptyStock = await _db.StockItems.Where(s => s.ShopId == id).ToListAsync(ct);
        _db.StockItems.RemoveRange(emptyStock);
        _db.RetailShops.Remove(shop);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    // ------------------------------------------------------------ employees

    public async Task<Result<OrgLists>> GetListsAsync(CancellationToken ct = default)
    {
        var shops = await _db.RetailShops
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ShopOption(s.Id, s.Name, s.OutletCode))
            .ToListAsync(ct);

        // Only staff-capable logins are offered: a customer account cannot be
        // turned into an employee by picking it here.
        var linked = await _db.Employees.AsNoTracking().Select(e => e.UserId).ToListAsync(ct);

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role != NexusRoles.Customer)
            .OrderBy(u => u.FullName)
            .Select(u => new AppUserOption(u.Id, u.AccountId, u.FullName, u.Role,
                                           linked.Contains(u.Id)))
            .ToListAsync(ct);

        return Result<OrgLists>.Ok(new OrgLists { Shops = shops, Users = users });
    }

    public async Task<Result<int>> SaveEmployeeAsync(EmployeeEditViewModel model, CancellationToken ct = default)
    {
        var code = model.EmployeeCode.Trim().ToUpperInvariant();

        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == code && e.Id != model.Id, ct))
            return Result<int>.Fail(nameof(model.EmployeeCode), $"Employee code '{code}' is already in use.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == model.UserId && !u.IsDeleted, ct);
        if (user is null)
            return Result<int>.Fail(nameof(model.UserId), "Pick a staff login to attach this record to.");

        if (await _db.Employees.AnyAsync(e => e.UserId == model.UserId && e.Id != model.Id, ct))
            return Result<int>.Fail(nameof(model.UserId), $"{user.FullName} already has an employee record.");

        var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        var shopId = model.ShopId is null or 0 ? (int?)null : model.ShopId;

        if (shopId is not null && !await _db.RetailShops.AnyAsync(s => s.Id == shopId, ct))
            return Result<int>.Fail(nameof(model.ShopId), "That outlet does not exist.");

        if (model.Id == 0)
        {
            var employee = new Employee
            {
                EmployeeCode = code,
                FullName = model.FullName.Trim(),
                Designation = model.Designation?.Trim() ?? string.Empty,
                Phone = model.Phone?.Trim() ?? string.Empty,
                Email = email,
                JoinedOn = model.JoinedOn,
                IsActive = model.IsActive,
                ShopId = shopId,
                UserId = model.UserId
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync(ct);
            return Result<int>.Ok(employee.Id);
        }

        var existing = await _db.Employees.FirstOrDefaultAsync(e => e.Id == model.Id, ct);
        if (existing is null) return Result<int>.NotFound("Employee");

        existing.EmployeeCode = code;
        existing.FullName = model.FullName.Trim();
        existing.Designation = model.Designation?.Trim() ?? string.Empty;
        existing.Phone = model.Phone?.Trim() ?? string.Empty;
        existing.Email = email;
        existing.JoinedOn = model.JoinedOn;
        existing.IsActive = model.IsActive;
        existing.ShopId = shopId;
        existing.UserId = model.UserId;

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(existing.Id);
    }

    public async Task<Result<bool>> SetEmployeeActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null) return Result<bool>.NotFound("Employee");

        employee.IsActive = active;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(active);
    }

    public async Task<Result<EmployeeRow>> GetEmployeeAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.Employees
            .AsNoTracking()
            .Include(x => x.Shop)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (e is null) return Result<EmployeeRow>.NotFound("Employee");

        return Result<EmployeeRow>.Ok(new EmployeeRow(
            e.Id, e.EmployeeCode, e.FullName, e.Designation, e.Phone, e.Email ?? string.Empty,
            e.Shop?.Name, e.User.Role, e.JoinedOn.ToString("dd MMM yyyy"), e.IsActive,
            e.ShopId, e.UserId, e.JoinedOn));
    }

    public async Task<IReadOnlyList<CityOption>> CityOptionsAsync(CancellationToken ct = default) =>
        await _db.Cities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityOption(c.Id, c.Name, c.Region))
            .ToListAsync(ct);
}

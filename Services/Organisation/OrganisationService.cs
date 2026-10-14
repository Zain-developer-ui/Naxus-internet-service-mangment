using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Abstractions;
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

    Task<Result<StaffLoginListPage>> StaffLoginsAsync(CancellationToken ct = default);
    Task<Result<StaffLoginEditViewModel>> GetStaffLoginAsync(int id, CancellationToken ct = default);
    Task<Result<StaffLoginEditViewModel>> NewStaffLoginAsync(CancellationToken ct = default);
    Task<Result<int>> SaveStaffLoginAsync(StaffLoginEditViewModel model, CancellationToken ct = default);
    Task<Result<bool>> SetStaffLoginActiveAsync(int id, bool active, CancellationToken ct = default);

    Task<IReadOnlyList<CityOption>> CityOptionsAsync(CancellationToken ct = default);
}

public sealed class OrganisationService : IOrganisationService
{
    private readonly NexusDbContext _db;
    private readonly IPasswordService _passwords;

    public OrganisationService(NexusDbContext db, IPasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

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

    // ------------------------------------------------------------ staff logins

    public async Task<Result<StaffLoginListPage>> StaffLoginsAsync(CancellationToken ct = default)
    {
        var logins = await _db.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role != NexusRoles.Customer)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.AccountId,
                u.FullName,
                u.Email,
                u.Role,
                u.Phone,
                u.IsActive,
                EmployeeCode = u.Employee != null ? u.Employee.EmployeeCode : null
            })
            .ToListAsync(ct);

        var rows = logins
            .Select(u => new StaffLoginRow(
                u.Id,
                u.AccountId,
                u.FullName,
                u.Email ?? string.Empty,
                NexusRoles.DisplayName(u.Role),
                u.Phone ?? string.Empty,
                u.IsActive,
                u.EmployeeCode))
            .ToList();

        return Result<StaffLoginListPage>.Ok(new StaffLoginListPage { Logins = rows });
    }

    public async Task<Result<StaffLoginEditViewModel>> GetStaffLoginAsync(int id,
                                                                          CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted && u.Role != NexusRoles.Customer, ct);

        if (user is null) return Result<StaffLoginEditViewModel>.NotFound("Staff login");

        return Result<StaffLoginEditViewModel>.Ok(new StaffLoginEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role,
            Phone = user.Phone ?? string.Empty,
            IsActive = user.IsActive
        });
    }

    public Task<Result<StaffLoginEditViewModel>> NewStaffLoginAsync(CancellationToken ct = default) =>
        Task.FromResult(Result<StaffLoginEditViewModel>.Ok(new StaffLoginEditViewModel()));

    public async Task<Result<int>> SaveStaffLoginAsync(StaffLoginEditViewModel model,
                                                       CancellationToken ct = default)
    {
        var email = model.Email.Trim();
        var role = model.Role;

        if (!NexusRoles.Staff.Contains(role))
            return Result<int>.Fail(nameof(model.Role), "Pick one of the back-office roles.");

        if (await _db.Users.AnyAsync(u => !u.IsDeleted && u.Email == email && u.Id != model.Id, ct))
            return Result<int>.Fail(nameof(model.Email), $"{email} already has a login.");

        var wantsPassword = model.Id == 0 || !string.IsNullOrWhiteSpace(model.Password);

        if (wantsPassword)
        {
            if (model.Password.Length < 8)
                return Result<int>.Fail(nameof(model.Password), "Password must be at least 8 characters.");

            if (model.Password != model.ConfirmPassword)
                return Result<int>.Fail(nameof(model.ConfirmPassword), "Passwords do not match.");
        }

        var phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

        if (model.Id == 0)
        {
            var created = new AppUser
            {
                AccountId = await NextStaffAccountIdAsync(role, ct),
                Email = email,
                FullName = model.FullName.Trim(),
                Role = role,
                Phone = phone,
                PasswordHash = _passwords.Hash(model.Password),
                IsActive = model.IsActive,
                MustChangePassword = false
            };

            _db.Users.Add(created);
            await _db.SaveChangesAsync(ct);
            return Result<int>.Ok(created.Id);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == model.Id && !u.IsDeleted, ct);
        if (user is null) return Result<int>.NotFound("Staff login");

        user.Email = email;
        user.FullName = model.FullName.Trim();
        user.Role = role;
        user.Phone = phone;
        user.IsActive = model.IsActive;

        if (wantsPassword)
            user.PasswordHash = _passwords.Hash(model.Password);

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(user.Id);
    }

    public async Task<Result<bool>> SetStaffLoginActiveAsync(int id, bool active,
                                                             CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
        if (user is null) return Result<bool>.NotFound("Staff login");

        // Locking the last administrator out would leave the back office with no
        // one able to unlock it, so that one switch stays put.
        if (!active && user.Role == NexusRoles.Admin &&
            !await _db.Users.AnyAsync(u => u.Id != id && !u.IsDeleted && u.IsActive &&
                                           u.Role == NexusRoles.Admin, ct))
            return Result<bool>.Conflict("This is the only active administrator.");

        user.IsActive = active;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(active);
    }

    /**
     * Staff account ids follow the seeded convention: a role letter then a
     * zero-padded sequence. The next number is read back off the table rather
     * than kept in a counter, so a restored database cannot hand out a duplicate.
     */
    private async Task<string> NextStaffAccountIdAsync(string role, CancellationToken ct)
    {
        var prefix = role switch
        {
            NexusRoles.Admin => "A",
            NexusRoles.Accounts => "F",
            NexusRoles.Technical => "T",
            _ => "R"
        };

        var taken = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.AccountId.StartsWith(prefix))
            .Select(u => u.AccountId)
            .ToListAsync(ct);

        var next = 1L;

        foreach (var id in taken)
        {
            if (id.Length == prefix.Length + 13 &&
                long.TryParse(id.AsSpan(prefix.Length), out var n) && n >= next)
                next = n + 1;
        }

        return prefix + next.ToString("D13");
    }

    public async Task<IReadOnlyList<CityOption>> CityOptionsAsync(CancellationToken ct = default) =>
        await _db.Cities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityOption(c.Id, c.Name, c.Region))
            .ToListAsync(ct);
}

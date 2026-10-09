using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Data;
using NEXUS.Domain.Procurement;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Procurement;

/**
 * Vendor register. Purchase ordering itself is a separate workflow; this
 * service only owns the supplier record the orders hang off.
 */
public interface IVendorService
{
    Task<Result<VendorListPage>> ListAsync(string? search, bool showInactive, CancellationToken ct = default);
    Task<Result<VendorEditViewModel>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveAsync(VendorEditViewModel model, CancellationToken ct = default);
    Task<Result<bool>> SetActiveAsync(int id, bool active, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class VendorService : IVendorService
{
    private readonly NexusDbContext _db;

    public VendorService(NexusDbContext db) => _db = db;

    public async Task<Result<VendorListPage>> ListAsync(string? search, bool showInactive,
                                                        CancellationToken ct = default)
    {
        var query = _db.Vendors.AsNoTracking().Where(v => !v.IsDeleted);

        if (!showInactive) query = query.Where(v => v.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(v =>
                EF.Functions.Like(v.Name, $"%{term}%") ||
                EF.Functions.Like(v.ContactPerson, $"%{term}%") ||
                EF.Functions.Like(v.TaxNumber, $"%{term}%"));
        }

        var rows = await query
            .OrderBy(v => v.Name)
            .Select(v => new VendorRow(
                v.Id,
                v.Name,
                v.ContactPerson,
                v.Phone,
                v.Email ?? string.Empty,
                v.Address,
                v.TaxNumber,
                v.IsActive,
                v.PurchaseOrders.Count(o => o.ReceivedOn == null && o.Status != "Cancelled"),
                v.PurchaseOrders.Count))
            .ToListAsync(ct);

        var live = _db.Vendors.AsNoTracking().Where(v => !v.IsDeleted);
        var totals = new VendorTotals(
            await live.CountAsync(ct),
            await live.CountAsync(v => v.IsActive, ct),
            await _db.PurchaseOrders
                .Where(o => o.ReceivedOn == null && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m);

        return Result<VendorListPage>.Ok(new VendorListPage
        {
            Vendors = rows,
            Totals = totals,
            Search = search,
            ShowInactive = showInactive
        });
    }

    public async Task<Result<VendorEditViewModel>> GetAsync(int id, CancellationToken ct = default)
    {
        var vendor = await _db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
        if (vendor is null) return Result<VendorEditViewModel>.NotFound("Vendor");

        return Result<VendorEditViewModel>.Ok(new VendorEditViewModel
        {
            Id = vendor.Id,
            Name = vendor.Name,
            ContactPerson = vendor.ContactPerson,
            Phone = vendor.Phone,
            Email = vendor.Email,
            Address = vendor.Address,
            TaxNumber = vendor.TaxNumber,
            IsActive = vendor.IsActive,
            PurchaseOrderCount = await _db.PurchaseOrders.CountAsync(o => o.VendorId == id, ct)
        });
    }

    public async Task<Result<int>> SaveAsync(VendorEditViewModel model, CancellationToken ct = default)
    {
        var name = model.Name.Trim();

        if (await _db.Vendors.AnyAsync(v => v.Name == name && v.Id != model.Id && !v.IsDeleted, ct))
            return Result<int>.Fail(nameof(model.Name), $"A vendor named '{name}' already exists.");

        var tax = model.TaxNumber?.Trim() ?? string.Empty;
        if (tax.Length > 0 &&
            await _db.Vendors.AnyAsync(v => v.TaxNumber == tax && v.Id != model.Id && !v.IsDeleted, ct))
            return Result<int>.Fail(nameof(model.TaxNumber), $"NTN '{tax}' is already registered to another vendor.");

        // An empty text input posts "" rather than null, so Email is folded
        // down to null before it reaches the unique-backing column.
        var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();

        if (model.Id == 0)
        {
            var vendor = new Vendor
            {
                Name = name,
                ContactPerson = model.ContactPerson?.Trim() ?? string.Empty,
                Phone = model.Phone?.Trim() ?? string.Empty,
                Email = email,
                Address = model.Address?.Trim() ?? string.Empty,
                TaxNumber = tax,
                IsActive = model.IsActive
            };

            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync(ct);
            return Result<int>.Ok(vendor.Id);
        }

        var existing = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == model.Id && !v.IsDeleted, ct);
        if (existing is null) return Result<int>.NotFound("Vendor");

        existing.Name = name;
        existing.ContactPerson = model.ContactPerson?.Trim() ?? string.Empty;
        existing.Phone = model.Phone?.Trim() ?? string.Empty;
        existing.Email = email;
        existing.Address = model.Address?.Trim() ?? string.Empty;
        existing.TaxNumber = tax;
        existing.IsActive = model.IsActive;

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(existing.Id);
    }

    public async Task<Result<bool>> SetActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
        if (vendor is null) return Result<bool>.NotFound("Vendor");

        vendor.IsActive = active;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(active);
    }

    /**
     * Soft delete. A vendor that has ever been ordered from stays in the
     * database because purchase orders reference it.
     */
    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
        if (vendor is null) return Result<bool>.NotFound("Vendor");

        if (await _db.PurchaseOrders.AnyAsync(o => o.VendorId == id, ct))
            return Result<bool>.Conflict(
                $"'{vendor.Name}' has purchase history. Set it inactive instead of deleting.");

        vendor.IsDeleted = true;
        vendor.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }
}

using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Data;
using NEXUS.Domain.Catalog;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Inventory;

/**
 * Equipment stock. Every change writes a movement row in the same transaction
 * as the quantity update, so the ledger and the on-hand figure can never
 * disagree - which is the whole point of tracking stock at all.
 */
public interface IInventoryService
{
    Task<Result<StockListPage>> ListStockAsync(int? shopId, string? search, bool lowOnly, CancellationToken ct = default);
    Task<Result<StockDetailPage>> GetStockAsync(int stockItemId, CancellationToken ct = default);
    Task<Result<int>> AdjustAsync(StockAdjustViewModel model, CancellationToken ct = default);

    Task<Result<ProductListPage>> ListProductsAsync(string? search, bool showInactive, CancellationToken ct = default);
    Task<Result<ProductEditViewModel>> GetProductAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveProductAsync(ProductEditViewModel model, CancellationToken ct = default);
    Task<Result<bool>> SetProductActiveAsync(int id, bool active, CancellationToken ct = default);
    Task<Result<bool>> DeleteProductAsync(int id, CancellationToken ct = default);
}

public sealed class InventoryService : IInventoryService
{
    private readonly NexusDbContext _db;

    public InventoryService(NexusDbContext db) => _db = db;

    public async Task<Result<StockListPage>> ListStockAsync(int? shopId, string? search, bool lowOnly,
                                                            CancellationToken ct = default)
    {
        IQueryable<StockItem> query = _db.StockItems
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Shop);

        if (shopId is not null)
            query = query.Where(s => s.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                EF.Functions.Like(s.Product.Name, $"%{term}%") ||
                EF.Functions.Like(s.Product.Sku, $"%{term}%"));
        }

        var rows = await query
            .OrderBy(s => s.Shop.Name)
            .ThenBy(s => s.Product.Name)
            .Select(s => new StockRow(
                s.Id,
                s.Product.Sku,
                s.Product.Name,
                s.Shop.Name,
                s.Shop.OutletCode,
                s.Quantity,
                s.Product.LowStockThreshold,
                s.Quantity < s.Product.LowStockThreshold,
                s.Product.IsActive))
            .ToListAsync(ct);

        if (lowOnly) rows = rows.Where(r => r.IsBelowThreshold).ToList();

        var all = _db.StockItems.AsNoTracking();
        var totals = new StockTotals(
            await all.Select(s => s.ProductId).Distinct().CountAsync(ct),
            await all.SumAsync(s => (int?)s.Quantity, ct) ?? 0,
            await all.CountAsync(s => s.Quantity < s.Product.LowStockThreshold, ct));

        var shops = await _db.RetailShops
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ShopOption(s.Id, s.Name, s.OutletCode))
            .ToListAsync(ct);

        return Result<StockListPage>.Ok(new StockListPage
        {
            Items = rows,
            Totals = totals,
            Shops = shops,
            ShopId = shopId,
            Search = search,
            LowOnly = lowOnly
        });
    }

    public async Task<Result<StockDetailPage>> GetStockAsync(int stockItemId, CancellationToken ct = default)
    {
        var item = await _db.StockItems
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Shop)
            .FirstOrDefaultAsync(s => s.Id == stockItemId, ct);

        if (item is null) return Result<StockDetailPage>.NotFound("Stock item");

        var movements = await _db.StockMovements
            .AsNoTracking()
            .Where(m => m.StockItemId == stockItemId)
            .OrderByDescending(m => m.OccurredAt)
            .ThenByDescending(m => m.Id)
            .Take(100)
            .Select(m => new StockMovementRow
            {
                Id = m.Id,
                QuantityDelta = m.QuantityDelta,
                QuantityAfter = m.QuantityAfter,
                Reason = m.Reason,
                ProductName = m.StockItem.Product.Name,
                ShopName = m.StockItem.Shop.Name,
                OccurredAt = m.OccurredAt.ToString("dd MMM yyyy, HH:mm"),
                PoNumber = m.PurchaseOrder != null ? m.PurchaseOrder.PoNumber : null
            })
            .ToListAsync(ct);

        return Result<StockDetailPage>.Ok(new StockDetailPage
        {
            Item = new StockRow(
                item.Id,
                item.Product.Sku,
                item.Product.Name,
                item.Shop.Name,
                item.Shop.OutletCode,
                item.Quantity,
                item.Product.LowStockThreshold,
                item.Quantity < item.Product.LowStockThreshold,
                item.Product.IsActive),
            Movements = movements,
            Adjust = new StockAdjustViewModel
            {
                StockItemId = item.Id,
                ProductName = item.Product.Name,
                ShopName = item.Shop.Name,
                QuantityOnHand = item.Quantity,
                LowStockThreshold = item.Product.LowStockThreshold
            }
        });
    }

    /**
     * Stock cannot go negative - issuing more than is on hand means the ledger
     * is wrong somewhere, and quietly clamping it would hide that.
     */
    public async Task<Result<int>> AdjustAsync(StockAdjustViewModel model, CancellationToken ct = default)
    {
        var item = await _db.StockItems
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == model.StockItemId, ct);

        if (item is null) return Result<int>.NotFound("Stock item");

        var after = item.Quantity + model.QuantityDelta;
        if (after < 0)
            return Result<int>.Fail(nameof(model.QuantityDelta),
                $"Only {item.Quantity} unit(s) on hand. Cannot remove {Math.Abs(model.QuantityDelta)}.");

        var reason = model.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return Result<int>.Fail(nameof(model.Reason), "A reason is required for every stock change.");

        item.Quantity = after;
        item.UpdatedAt = DateTime.UtcNow;

        _db.StockMovements.Add(new StockMovement
        {
            StockItemId = item.Id,
            QuantityDelta = model.QuantityDelta,
            QuantityAfter = after,
            Reason = reason,
            OccurredAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return Result<int>.Ok(after);
    }

    // ------------------------------------------------------------- products

    public async Task<Result<ProductListPage>> ListProductsAsync(string? search, bool showInactive,
                                                                 CancellationToken ct = default)
    {
        var query = _db.EquipmentProducts.AsNoTracking().AsQueryable();

        if (!showInactive) query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{term}%") ||
                EF.Functions.Like(p.Sku, $"%{term}%"));
        }

        var rows = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductRow(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.UnitPrice,
                p.LowStockThreshold,
                p.IsActive,
                p.Stock.Sum(s => (int?)s.Quantity) ?? 0,
                p.Stock.Count))
            .ToListAsync(ct);

        return Result<ProductListPage>.Ok(new ProductListPage
        {
            Products = rows,
            Search = search,
            ShowInactive = showInactive
        });
    }

    public async Task<Result<ProductEditViewModel>> GetProductAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.EquipmentProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return Result<ProductEditViewModel>.NotFound("Product");

        return Result<ProductEditViewModel>.Ok(new ProductEditViewModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            UnitPrice = product.UnitPrice,
            LowStockThreshold = product.LowStockThreshold,
            IsActive = product.IsActive
        });
    }

    public async Task<Result<int>> SaveProductAsync(ProductEditViewModel model, CancellationToken ct = default)
    {
        var sku = model.Sku.Trim();

        if (await _db.EquipmentProducts.AnyAsync(p => p.Sku == sku && p.Id != model.Id, ct))
            return Result<int>.Fail(nameof(model.Sku), $"SKU '{sku}' is already in use.");

        if (model.Id == 0)
        {
            var product = new EquipmentProduct
            {
                Sku = sku,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim() ?? string.Empty,
                UnitPrice = model.UnitPrice,
                LowStockThreshold = model.LowStockThreshold,
                IsActive = model.IsActive
            };

            _db.EquipmentProducts.Add(product);
            await _db.SaveChangesAsync(ct);
            return Result<int>.Ok(product.Id);
        }

        var existing = await _db.EquipmentProducts.FirstOrDefaultAsync(p => p.Id == model.Id, ct);
        if (existing is null) return Result<int>.NotFound("Product");

        existing.Sku = sku;
        existing.Name = model.Name.Trim();
        existing.Description = model.Description?.Trim() ?? string.Empty;
        existing.UnitPrice = model.UnitPrice;
        existing.LowStockThreshold = model.LowStockThreshold;
        existing.IsActive = model.IsActive;

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(existing.Id);
    }

    public async Task<Result<bool>> SetProductActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var product = await _db.EquipmentProducts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return Result<bool>.NotFound("Product");

        product.IsActive = active;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(active);
    }

    /**
     * A product with stock anywhere, or with purchase history, is retired
     * rather than removed - the movement ledger points back at it.
     */
    public async Task<Result<bool>> DeleteProductAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.EquipmentProducts
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null) return Result<bool>.NotFound("Product");

        if (product.Stock.Any(s => s.Quantity > 0))
            return Result<bool>.Conflict(
                $"'{product.Name}' still has stock on hand. Draw it down before deleting.");

        if (await _db.PurchaseOrderLines.AnyAsync(l => l.ProductId == id, ct))
            return Result<bool>.Conflict(
                $"'{product.Name}' appears on past purchase orders. Set it inactive instead.");

        var empty = product.Stock.ToList();
        _db.StockItems.RemoveRange(empty);
        _db.EquipmentProducts.Remove(product);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }
}

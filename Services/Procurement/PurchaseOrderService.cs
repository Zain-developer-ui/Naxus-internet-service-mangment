using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Data;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Procurement;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Generation;

namespace NEXUS.Services.Procurement;

/**
 * Purchase orders: what the admin has asked a vendor to send, and what has
 * actually turned up. Receiving is the only path that puts stock on the
 * shelf, and it does so in one transaction with the ledger row - the same
 * rule the stock screen follows.
 */
public interface IPurchaseOrderService
{
    Task<Result<PurchaseOrderListPage>> ListAsync(string? status, int? vendorId, CancellationToken ct = default);
    Task<Result<PurchaseOrderEditViewModel>> GetOptionsAsync(CancellationToken ct = default);
    Task<Result<PurchaseOrderEditViewModel>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(PurchaseOrderEditViewModel model, CancellationToken ct = default);
    Task<Result<PurchaseOrderDetailPage>> GetDetailAsync(int id, CancellationToken ct = default);
    Task<Result<bool>> ReceiveAsync(int id, int shopId, CancellationToken ct = default);
    Task<Result<bool>> CancelAsync(int id, CancellationToken ct = default);
}

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    public const string Draft = "Draft";
    public const string Ordered = "Ordered";
    public const string Received = "Received";
    public const string PartiallyReceived = "PartiallyReceived";
    public const string Cancelled = "Cancelled";

    private static readonly string[] AllStatuses =
        { Draft, Ordered, PartiallyReceived, Received, Cancelled };

    private readonly NexusDbContext _db;
    private readonly IAccountIdGenerator _ids;

    public PurchaseOrderService(NexusDbContext db, IAccountIdGenerator ids)
    {
        _db = db;
        _ids = ids;
    }

    public async Task<Result<PurchaseOrderListPage>> ListAsync(string? status, int? vendorId,
                                                              CancellationToken ct = default)
    {
        IQueryable<PurchaseOrder> query = _db.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Lines);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (vendorId is not null)
            query = query.Where(p => p.VendorId == vendorId);

        var orders = await query
            .OrderByDescending(p => p.OrderedOn)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);

        var rows = orders.Select(ToRow).ToList();

        var totals = new PurchaseTotals(
            rows.Count,
            rows.Count(r => r.CanReceive),
            rows.Count(r => r.StatusToken == Received),
            rows.Where(r => r.StatusToken != Cancelled).Sum(r => r.TotalAmount));

        var vendors = await _db.Vendors
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .Select(v => new VendorOption(v.Id, v.Name))
            .ToListAsync(ct);

        return Result<PurchaseOrderListPage>.Ok(new PurchaseOrderListPage
        {
            Orders = rows,
            Totals = totals,
            Status = status,
            VendorId = vendorId,
            Vendors = vendors,
            Statuses = AllStatuses
        });
    }

    public async Task<Result<PurchaseOrderEditViewModel>> GetOptionsAsync(CancellationToken ct = default)
    {
        var model = new PurchaseOrderEditViewModel();
        await FillOptionsAsync(model, ct);
        return Result<PurchaseOrderEditViewModel>.Ok(model);
    }

    public async Task<Result<PurchaseOrderEditViewModel>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (order is null) return Result<PurchaseOrderEditViewModel>.NotFound("Purchase order");

        var model = new PurchaseOrderEditViewModel
        {
            Id = order.Id,
            VendorId = order.VendorId,
            ExpectedOn = order.ExpectedOn,
            Notes = order.Notes,
            PoNumber = order.PoNumber,
            Status = order.Status,
            Lines = order.Lines.Select(l => new PurchaseLineInput
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                QuantityReceived = l.QuantityReceived
            }).ToList()
        };

        await FillOptionsAsync(model, ct);
        return Result<PurchaseOrderEditViewModel>.Ok(model);
    }

    public async Task<Result<int>> CreateAsync(PurchaseOrderEditViewModel model, CancellationToken ct = default)
    {
        if (!await _db.Vendors.AnyAsync(v => v.Id == model.VendorId && v.IsActive, ct))
            return Result<int>.Fail(nameof(model.VendorId), "Pick an active vendor.");

        /**
         * The picker writes a hidden blank line for its "add row" template. Drop
         * rows with no product and no quantity rather than rejecting the whole
         * order on a placeholder the user never touched.
         */
        var lines = model.Lines
            .Where(l => l.ProductId > 0 && l.Quantity > 0)
            .ToList();

        if (lines.Count == 0)
            return Result<int>.Fail(nameof(model.Lines), "Add at least one line to the order.");

        var duplicate = lines.GroupBy(l => l.ProductId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            var name = await _db.EquipmentProducts
                .Where(p => p.Id == duplicate.Key)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct);
            return Result<int>.Fail(nameof(model.Lines), $"'{name}' appears on more than one line.");
        }

        var productIds = lines.Select(l => l.ProductId).ToList();
        var known = await _db.EquipmentProducts
            .Where(p => productIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var unknown = productIds.Except(known).ToList();
        if (unknown.Count > 0)
            return Result<int>.Fail(nameof(model.Lines), "One of the selected products no longer exists.");

        var order = new PurchaseOrder
        {
            PoNumber = await _ids.NextPurchaseOrderNumberAsync(ct),
            VendorId = model.VendorId,
            OrderedOn = DateTime.UtcNow.Date,
            ExpectedOn = model.ExpectedOn,
            /**
             * Raising an order is the commitment; there is no separate draft
             * step in this flow, so a saved order is always live.
             */
            Status = Ordered,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim()
        };

        foreach (var line in lines)
        {
            order.Lines.Add(new PurchaseOrderLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineAmount = line.Quantity * line.UnitCost
            });
        }

        order.TotalAmount = order.Lines.Sum(l => l.LineAmount);

        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        return Result<int>.Ok(order.Id);
    }

    public async Task<Result<PurchaseOrderDetailPage>> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (order is null) return Result<PurchaseOrderDetailPage>.NotFound("Purchase order");

        var shops = await _db.RetailShops
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ShopOption(s.Id, s.Name, s.OutletCode))
            .ToListAsync(ct);

        return Result<PurchaseOrderDetailPage>.Ok(new PurchaseOrderDetailPage
        {
            Order = ToRow(order),
            Notes = order.Notes,
            Shops = shops,
            Lines = order.Lines.Select(l => new PurchaseLineRow(
                l.Id,
                l.Product.Sku,
                l.Product.Name,
                l.Quantity,
                l.QuantityReceived,
                l.UnitCost,
                l.LineAmount,
                l.QuantityReceived >= l.Quantity)).ToList()
        });
    }

    /**
     * Receiving books every line into one shop and marks the whole order
     * received. Partial arrival is a real case but splitting one order across
     * shops and dates needs a receiving screen per delivery, so for now the
     * order lands whole - and only once, which the status guard enforces.
     */
    public async Task<Result<bool>> ReceiveAsync(int id, int shopId, CancellationToken ct = default)
    {
        var order = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (order is null) return Result<bool>.NotFound("Purchase order");

        if (order.Status is Cancelled)
            return Result<bool>.Conflict("A cancelled order cannot be received.");

        if (order.Status is Received)
            return Result<bool>.Conflict("This order has already been received.");

        if (!await _db.RetailShops.AnyAsync(s => s.Id == shopId, ct))
            return Result<bool>.Fail("shopId", "Pick the outlet the goods arrived at.");

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var now = DateTime.UtcNow;

            foreach (var line in order.Lines)
            {
                var outstanding = line.Quantity - line.QuantityReceived;
                if (outstanding <= 0) continue;

                var item = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.ProductId == line.ProductId && s.ShopId == shopId, ct);

                if (item is null)
                {
                    item = new StockItem { ProductId = line.ProductId, ShopId = shopId, Quantity = 0 };
                    _db.StockItems.Add(item);
                }

                item.Quantity += outstanding;
                item.UpdatedAt = now;

                _db.StockMovements.Add(new StockMovement
                {
                    StockItem = item,
                    QuantityDelta = outstanding,
                    QuantityAfter = item.Quantity,
                    Reason = $"Received on {order.PoNumber}",
                    PurchaseOrderId = order.Id,
                    OccurredAt = now
                });

                line.QuantityReceived = line.Quantity;
                line.UpdatedAt = now;
            }

            order.Status = Received;
            order.ReceivedOn = now;
            order.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Result<bool>.Ok(true);
        });
    }

    public async Task<Result<bool>> CancelAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (order is null) return Result<bool>.NotFound("Purchase order");

        if (order.Status is Received)
            return Result<bool>.Conflict("Received stock cannot be un-done from here. Draw it down on the stock screen.");

        if (order.Status is Cancelled)
            return Result<bool>.Conflict("This order is already cancelled.");

        order.Status = Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    private async Task FillOptionsAsync(PurchaseOrderEditViewModel model, CancellationToken ct)
    {
        model.Vendors = await _db.Vendors
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .Select(v => new VendorOption(v.Id, v.Name))
            .ToListAsync(ct);

        model.Products = await _db.EquipmentProducts
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProductOption(p.Id, p.Sku, p.Name, p.UnitPrice ?? 0m))
            .ToListAsync(ct);
    }

    private static PurchaseOrderRow ToRow(PurchaseOrder p)
    {
        var ordered = p.Lines.Sum(l => l.Quantity);
        var received = p.Lines.Sum(l => l.QuantityReceived);

        return new PurchaseOrderRow(
            p.Id,
            p.PoNumber,
            p.Vendor.Name,
            StatusLabel(p.Status),
            p.Status,
            p.Lines.Count,
            ordered,
            received,
            p.TotalAmount,
            p.OrderedOn,
            p.ExpectedOn,
            p.ReceivedOn,
            ordered > 0 && received >= ordered,
            p.Status is Ordered or Draft or PartiallyReceived,
            p.Status is not Received and not Cancelled);
    }

    private static string StatusLabel(string status) => status switch
    {
        PartiallyReceived => "Partially received",
        _ => status
    };
}

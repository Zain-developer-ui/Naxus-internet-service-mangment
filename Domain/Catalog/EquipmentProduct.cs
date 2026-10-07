using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Organisation;
using NEXUS.Domain.Procurement;

namespace NEXUS.Domain.Catalog;

public class EquipmentProduct : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Sku { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /**
     * The SRS never prices equipment - it only says the customer is charged
     * for replacement if they damage it. Left nullable on purpose.
     */
    public decimal? UnitPrice { get; set; }

    public int LowStockThreshold { get; set; } = 5;

    public bool IsActive { get; set; } = true;

    public ICollection<StockItem> Stock { get; set; } = new List<StockItem>();
    public ICollection<PurchaseOrderLine> PurchaseLines { get; set; } = new List<PurchaseOrderLine>();
}

public class StockItem : AuditableEntity, IConcurrencyGuarded
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public EquipmentProduct Product { get; set; } = null!;

    public int ShopId { get; set; }
    public RetailShop Shop { get; set; } = null!;

    public int Quantity { get; set; }

    /** Two staff adjusting the same stock row must not overwrite each other. */
    public byte[]? RowVersion { get; set; }

    public bool IsBelowThreshold => Quantity < Product.LowStockThreshold;
}

public class StockMovement : AuditableEntity
{
    public int Id { get; set; }

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /** Positive for receipts and returns, negative for issues. */
    public int QuantityDelta { get; set; }

    public int QuantityAfter { get; set; }

    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

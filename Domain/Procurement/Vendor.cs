using System.ComponentModel.DataAnnotations;

namespace NEXUS.Domain.Procurement;

public class Vendor : AuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ContactPerson { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Email { get; set; }

    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(40)]
    public string TaxNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

public class PurchaseOrder : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string PoNumber { get; set; } = string.Empty;

    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public DateTime OrderedOn { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ExpectedOn { get; set; }
    public DateTime? ReceivedOn { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Draft";

    [MaxLength(300)]
    public string? Notes { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}

public class PurchaseOrderLine : AuditableEntity
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int ProductId { get; set; }
    public Catalog.EquipmentProduct Product { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineAmount { get; set; }

    public int QuantityReceived { get; set; }
}

public class Feedback : AuditableEntity
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customers.Customer Customer { get; set; } = null!;

    public int? OrderId { get; set; }
    public Orders.ConnectionOrder? Order { get; set; }

    /** One to five stars, validated at the form and again in the service. */
    public int Rating { get; set; }

    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    [MaxLength(500)]
    public string? Response { get; set; }

    public DateTime? RespondedAt { get; set; }
}

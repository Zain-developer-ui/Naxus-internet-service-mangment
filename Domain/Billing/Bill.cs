using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Orders;

namespace NEXUS.Domain.Billing;

public class Bill : AuditableEntity, IConcurrencyGuarded
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string BillNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? ConnectionId { get; set; }
    public Connection? Connection { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow.Date;
    public DateTime DueOn { get; set; }

    public BillStatus Status { get; set; } = BillStatus.Draft;

    /** Sum of the lines, before any discount. */
    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxableAmount { get; set; }

    public decimal ServiceTaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal OutstandingAmount => TotalAmount - AmountPaid;

    [MaxLength(300)]
    public string? Notes { get; set; }

    public byte[]? RowVersion { get; set; }

    public ICollection<BillLine> Lines { get; set; } = new List<BillLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public bool IsFullyPaid => AmountPaid >= TotalAmount;

    public string StatusLabel => Status.DisplayName();
    public string StatusBadge => Status.BadgeClass();
}

public class BillLine : AuditableEntity
{
    public int Id { get; set; }

    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(30)]
    public string ChargeType { get; set; } = "Rental";

    public decimal Quantity { get; set; } = 1;

    public decimal UnitAmount { get; set; }

    public decimal LineAmount { get; set; }

    public int SortOrder { get; set; }
}

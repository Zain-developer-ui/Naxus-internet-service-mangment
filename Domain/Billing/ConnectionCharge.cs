using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Billing;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Orders;
using NEXUS.Domain.Organisation;

namespace NEXUS.Domain.Billing;

/**
 * A one-off charge raised against a connection outside the monthly cycle.
 *
 * The SRS only names two of these - a replacement charge when the customer
 * damages equipment (FR-101) and any manual adjustment accounts need to make -
 * so the table is deliberately small: an amount, a reason and a link back to
 * whoever raised it.
 *
 * These sit pending until a bill sweeps them up. That is why there is no
 * "discard" habit here: once written onto a bill the row is stamped with the
 * bill id and cannot be picked up again.
 */
public class ConnectionCharge : AuditableEntity
{
    public int Id { get; set; }

    public int ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /** Mirrors ChargeTypes on a bill line so the two stay recognisable. */
    [Required, MaxLength(30)]
    public string ChargeType { get; set; } = ChargeTypes.Replacement;

    public decimal Amount { get; set; }

    /** Null while pending; set to the bill that carried it once billed. */
    public int? BilledOnBillId { get; set; }
    public Bill? BilledOnBill { get; set; }

    public int? RaisedByEmployeeId { get; set; }
    public Employee? RaisedBy { get; set; }

    public bool IsPending => BilledOnBillId is null;

    public string AmountLabel => Amount.ToString("C");
}

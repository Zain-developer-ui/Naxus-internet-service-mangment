using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Organisation;

namespace NEXUS.Domain.Billing;

/**
 * Both the Accounts department and a retail outlet can take money. Which one
 * did is recorded, because the SRS keeps the two channels separate in its
 * reporting.
 */
public class Payment : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string ReceiptNumber { get; set; } = string.Empty;

    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public DateTime PaidOn { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(300)]
    public string? Remarks { get; set; }

    public int? ReceivedByShopId { get; set; }
    public RetailShop? ReceivedByShop { get; set; }

    public int? ReceivedByUserId { get; set; }
    public AppUser? ReceivedBy { get; set; }

    public string MethodLabel => Method.DisplayName();
}

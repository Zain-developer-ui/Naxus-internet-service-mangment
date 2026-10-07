using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Services.Billing;

namespace NEXUS.Models.ViewModels;

public class BillListPage
{
    public IReadOnlyList<BillSummary> Bills { get; set; } = Array.Empty<BillSummary>();

    /** A customer sees their own account; staff see the whole ledger. */
    public bool IsCustomerView { get; set; }

    public BillStatus? Filter { get; set; }
    public string? Search { get; set; }

    public decimal TotalBilled => Bills.Sum(b => b.TotalAmount);
    public decimal TotalPaid => Bills.Sum(b => b.AmountPaid);
    public decimal TotalOutstanding => Bills.Sum(b => b.OutstandingAmount);
    public int PaidCount => Bills.Count(b => b.Status == BillStatus.Paid);
}

public class BillDetailPage
{
    public BillDetail Bill { get; set; } = null!;
    public IReadOnlyList<PaymentRecord> Payments { get; set; } = Array.Empty<PaymentRecord>();
    public bool CanRecordPayment { get; set; }

    public PaymentEntry Payment { get; set; } = new();
    public decimal Outstanding => Bill.Bill.OutstandingAmount;
}

public class PaymentEntry
{
    [Required]
    public int BillId { get; set; }

    [Required]
    [Range(0.01, 1_000_000, ErrorMessage = "Enter an amount greater than zero.")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    /** Required for cheques; the service enforces it too. */
    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(300)]
    public string? Remarks { get; set; }
}

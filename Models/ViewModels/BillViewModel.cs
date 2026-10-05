namespace NEXUS.Models.ViewModels
{
    public class BillViewModel
    {
        public string BillNo { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // Paid / Due / Overdue
        public DateTime DueDate { get; set; }
        public DateTime? PaidOn { get; set; }
    }

    public class BillDetailsViewModel
    {
        public string BillNo { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;

        public decimal MonthlyRental { get; set; }
        public decimal InstallationCharges { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal PreviousDues { get; set; }
        public decimal Taxes { get; set; }
        public decimal Discounts { get; set; }
        public decimal Adjustments { get; set; }

        public decimal SubTotal =>
            MonthlyRental + InstallationCharges + SecurityDeposit + PreviousDues + Adjustments;

        public decimal Total => SubTotal + Taxes - Discounts;

        public List<PaymentHistoryViewModel> PaymentHistory { get; set; } = new();
    }

    public class PaymentHistoryViewModel
    {
        public string BillNo { get; set; } = string.Empty;
        public DateTime PaidOn { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
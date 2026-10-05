namespace NEXUS.Models.ViewModels
{
    /// <summary>
    /// Bundled container used by the Customer dashboard to feed
    /// cards, tables and charts in one strongly-typed view.
    /// </summary>
    public class CustomerDashboardViewModel
    {
        public AccountViewModel Account { get; set; } = new();
        public BillViewModel? CurrentBill { get; set; }
        public List<BillViewModel> RecentBills { get; set; } = new();
        public List<PaymentHistoryViewModel> RecentPayments { get; set; } = new();
        public List<ActivityViewModel> RecentActivity { get; set; } = new();

        // Chart data (monthly MB usage in the last 6 months)
        public List<string> UsageLabels { get; set; } = new();
        public List<double> UsageValues { get; set; } = new();

        public List<string> PaymentLabels { get; set; } = new();
        public List<double> PaymentValues { get; set; } = new();
    }

    public class ActivityViewModel
    {
        public string Icon { get; set; } = "fa-circle-info";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string When { get; set; } = string.Empty;
        public string Tone { get; set; } = "info"; // info / success / warning / danger
    }
}
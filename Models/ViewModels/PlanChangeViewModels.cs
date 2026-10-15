namespace NEXUS.Models.ViewModels
{
    public class PlanChangePage
    {
        public int ConnectionId { get; set; }
        public string ConnectionNumber { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = string.Empty;
        public string CurrentPlanName { get; set; } = string.Empty;
        public decimal CurrentMonthlyRate { get; set; }

        public IReadOnlyList<PlanOptionRow> Plans { get; set; } = Array.Empty<PlanOptionRow>();
        public bool HasPendingRequest { get; set; }

        public PlanChangeInput Input { get; set; } = new();
    }

    public sealed record PlanOptionRow(int PlanId, string PlanName, decimal MonthlyRate,
                                       decimal SecurityDeposit);

    public class PlanChangeInput
    {
        public int ConnectionId { get; set; }

        public int PlanId { get; set; }

        public string? Remarks { get; set; }
    }

    public sealed record PlanChangeRow(int RequestId, string ConnectionNumber, string AccountId,
                                       string CustomerName, string CurrentPlan,
                                       string RequestedPlan, decimal RequestedMonthlyRate,
                                       DateTime RequestedAt);
}

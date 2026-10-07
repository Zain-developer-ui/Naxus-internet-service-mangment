namespace NEXUS.Models.ViewModels;

/**
 * Accounts console. Every figure is counted from Bills and Payments; the
 * collection rate is derived from real received amount rather than a constant.
 */
public sealed class AccountsDashboardData
{
    public decimal CollectedToday { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal OutstandingAmount { get; set; }

    public int PaidBills { get; set; }
    public int PendingBills { get; set; }
    public int OverdueBills { get; set; }
    public int TotalCustomers { get; set; }

    public decimal BilledThisCycle { get; set; }
    public decimal CollectionRatePercent { get; set; }

    public IReadOnlyList<AccountsPaymentRow> RecentPayments { get; set; } = Array.Empty<AccountsPaymentRow>();
    public IReadOnlyList<AccountsBillRow> Outstanding { get; set; } = Array.Empty<AccountsBillRow>();

    public bool HasAnyPayment => RecentPayments.Count > 0;
    public bool HasAnyOutstanding => Outstanding.Count > 0;
}

public sealed record AccountsPaymentRow(
    string ReceiptNumber,
    string CustomerName,
    string CustomerAccountId,
    string BillNumber,
    decimal Amount,
    string MethodName,
    DateTime PaidOn);

public sealed record AccountsBillRow(
    string BillNumber,
    string CustomerName,
    string CustomerAccountId,
    decimal OutstandingAmount,
    DateTime DueDate,
    string StatusLabel,
    string StatusToken);

/**
 * Technical console. The queue and the ticket list are the two things a
 * technician actually works from, so both are real order and connection rows.
 */
public sealed class TechnicalDashboardData
{
    public int ActiveConnections { get; set; }
    public int InstallationQueue { get; set; }
    public int CompletedInstallations { get; set; }
    public int SuspendedLines { get; set; }

    public IReadOnlyList<TechnicalQueueRow> Queue { get; set; } = Array.Empty<TechnicalQueueRow>();
    public IReadOnlyList<TechnicalConnectionRow> RecentConnections { get; set; } = Array.Empty<TechnicalConnectionRow>();

    public bool HasAnyQueue => Queue.Count > 0;
    public bool HasAnyConnection => RecentConnections.Count > 0;
}

public sealed record TechnicalQueueRow(
    string OrderId,
    string CustomerName,
    string CustomerAccountId,
    string PlanName,
    string ServiceTypeName,
    string CityName,
    string StatusLabel,
    string StatusToken,
    DateTime CreatedAt);

public sealed record TechnicalConnectionRow(
    string ConnectionNumber,
    string CustomerName,
    string ServiceTypeName,
    string StatusLabel,
    string StatusToken,
    DateTime? ActivatedOn);

/**
 * Retail console. "Today" means the calendar day in local time, which is what a
 * branch clerk means by it - a rolling 24 hours would read wrong first thing in
 * the morning.
 */
public sealed class RetailDashboardData
{
    public int NewCustomersToday { get; set; }
    public int OrdersToday { get; set; }
    public int OrdersThisWeek { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal SalesThisWeek { get; set; }

    public IReadOnlyList<RetailOrderRow> RecentOrders { get; set; } = Array.Empty<RetailOrderRow>();

    public bool HasAnyOrder => RecentOrders.Count > 0;
}

public sealed record RetailOrderRow(
    string OrderId,
    string CustomerName,
    string CustomerAccountId,
    string PlanName,
    string ServiceTypeName,
    string CityName,
    string StatusLabel,
    string StatusToken,
    DateTime CreatedAt);

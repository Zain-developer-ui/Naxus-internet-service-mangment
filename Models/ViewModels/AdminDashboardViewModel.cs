namespace NEXUS.Models.ViewModels;

/**
 * Live figures for the admin overview. Counts are nullable-free integers taken
 * straight from the database; the charts stay empty until a reporting endpoint
 * exists rather than showing an invented trend line.
 */
public sealed class AdminDashboardData
{
    public int TotalCustomers { get; set; }
    public int ActiveConnections { get; set; }
    public int PendingOrders { get; set; }
    public int ActiveInstallations { get; set; }
    public int OutstandingBills { get; set; }
    public int SuccessfulPayments { get; set; }

    public decimal MonthlyRevenue { get; set; }
    public decimal OutstandingAmount { get; set; }

    public int BroadbandConnections { get; set; }
    public int DialUpConnections { get; set; }
    public int TelephoneConnections { get; set; }

    public IReadOnlyList<RecentOrderRow> RecentOrders { get; set; } = Array.Empty<RecentOrderRow>();
    public IReadOnlyList<RecentPaymentRow> RecentPayments { get; set; } = Array.Empty<RecentPaymentRow>();
    public IReadOnlyList<RecentCustomerRow> RecentCustomers { get; set; } = Array.Empty<RecentCustomerRow>();
    public IReadOnlyList<RecentConnectionRow> RecentConnections { get; set; } = Array.Empty<RecentConnectionRow>();

    public int TotalConnections => BroadbandConnections + DialUpConnections + TelephoneConnections;
    public bool HasAnyConnection => TotalConnections > 0;
    public bool HasAnyOrder => RecentOrders.Count > 0;
    public bool HasAnyPayment => RecentPayments.Count > 0;
    public bool HasAnyCustomer => RecentCustomers.Count > 0;
}

public sealed record RecentOrderRow(
    string OrderId,
    string CustomerName,
    string CustomerAccountId,
    string PlanName,
    string CityName,
    string StatusLabel,
    string StatusToken,
    DateTime CreatedAt);

public sealed record RecentPaymentRow(
    string CustomerName,
    string ReceiptNumber,
    decimal Amount,
    string MethodName,
    DateTime PaidOn);

public sealed record RecentCustomerRow(
    string AccountId,
    string FullName,
    string ConnectionTypeName,
    string CityName,
    DateTime CreatedAt);

public sealed record RecentConnectionRow(
    string ConnectionNumber,
    string CustomerName,
    string CustomerAccountId,
    string PlanName,
    string CityName,
    string StatusLabel,
    string StatusToken,
    bool IsBillable);

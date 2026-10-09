namespace NEXUS.Models.ViewModels;

/**
 * Live figures for the admin overview. Counts come straight from the database;
 * the trend series is handed in from the report service so the dashboard shows
 * the same numbers the reports page does.
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

    public IReadOnlyList<TrendPoint> Trend { get; set; } = Array.Empty<TrendPoint>();

    public IReadOnlyList<RecentOrderRow> RecentOrders { get; set; } = Array.Empty<RecentOrderRow>();
    public IReadOnlyList<RecentPaymentRow> RecentPayments { get; set; } = Array.Empty<RecentPaymentRow>();
    public IReadOnlyList<RecentCustomerRow> RecentCustomers { get; set; } = Array.Empty<RecentCustomerRow>();
    public IReadOnlyList<RecentConnectionRow> RecentConnections { get; set; } = Array.Empty<RecentConnectionRow>();

    public int TotalConnections => BroadbandConnections + DialUpConnections + TelephoneConnections;
    public bool HasAnyConnection => TotalConnections > 0;
    public bool HasAnyOrder => RecentOrders.Count > 0;
    public bool HasAnyPayment => RecentPayments.Count > 0;
    public bool HasAnyCustomer => RecentCustomers.Count > 0;

    // A flat run of months is not a trend worth drawing, and a series that is
    // zero everywhere is a blank rectangle under a heading.
    public bool HasTrend => HasRevenue || HasOrders;
    public bool HasRevenue => Trend.Any(p => p.Revenue > 0);
    public bool HasOrders => Trend.Any(p => p.Orders > 0);
}

public sealed record TrendPoint(string Label, decimal Revenue, int Orders);

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

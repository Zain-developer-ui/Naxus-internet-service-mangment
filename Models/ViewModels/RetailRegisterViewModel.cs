namespace NEXUS.Models.ViewModels;

/**
 * The retail register. The SRS asks a retail outlet to keep track of the orders
 * placed to date, the connections those orders became, and the billing and
 * payment position against them - so all four sit on one screen behind one
 * period filter rather than four separate trips.
 */
public sealed class RetailRegisterQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public bool HasRange => From is not null || To is not null;

    public string FromValue => From?.ToString("yyyy-MM-dd") ?? string.Empty;
    public string ToValue => To?.ToString("yyyy-MM-dd") ?? string.Empty;

    public string PeriodLabel
    {
        get
        {
            if (From is null && To is null) return "All time";
            if (From is not null && To is not null) return $"{From:dd MMM yyyy} - {To:dd MMM yyyy}";
            if (From is not null) return $"From {From:dd MMM yyyy}";
            return $"Up to {To:dd MMM yyyy}";
        }
    }
}

public sealed class RetailRegisterTotals(
    int Orders, int Connections, decimal Billed, decimal Collected, decimal Outstanding)
{
    public int Orders { get; } = Orders;
    public int Connections { get; } = Connections;
    public decimal Billed { get; } = Billed;
    public decimal Collected { get; } = Collected;
    public decimal Outstanding { get; } = Outstanding;
}

public sealed class RetailRegisterPage
{
    public RetailRegisterQuery Query { get; init; } = new();

    public IReadOnlyList<SearchOrderRow> Orders { get; init; } = Array.Empty<SearchOrderRow>();
    public IReadOnlyList<SearchConnectionRow> Connections { get; init; } = Array.Empty<SearchConnectionRow>();
    public IReadOnlyList<RegisterBillRow> Bills { get; init; } = Array.Empty<RegisterBillRow>();
    public IReadOnlyList<RegisterPaymentRow> Payments { get; init; } = Array.Empty<RegisterPaymentRow>();

    public RetailRegisterTotals Totals { get; init; } = new(0, 0, 0m, 0m, 0m);
}

public sealed record RegisterBillRow(
    int BillId,
    string BillNumber,
    string CustomerName,
    string AccountId,
    string Period,
    decimal Total,
    decimal Paid,
    decimal Balance,
    string Status,
    string StatusToken,
    DateTime DueOn);

public sealed record RegisterPaymentRow(
    string ReceiptNumber,
    string BillNumber,
    string CustomerName,
    decimal Amount,
    string Method,
    DateTime PaidOn);

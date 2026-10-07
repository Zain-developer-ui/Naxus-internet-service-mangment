namespace NEXUS.Common.Constants;

public enum ConnectionType
{
    DialUp = 1,
    Broadband = 2,
    Telephone = 3
}

public enum ConnectionStatus
{
    Pending = 0,
    Active = 1,
    TemporarilyInactive = 2,
    PermanentlyInactive = 3
}

public enum OrderStatus
{
    Draft = 0,
    AwaitingFeasibility = 1,
    FeasibilityPassed = 2,
    FeasibilityFailed = 3,
    Confirmed = 4,
    Installing = 5,
    Completed = 6,
    Cancelled = 7
}

public enum FeasibilityResult
{
    Pending = 0,
    Passed = 1,
    Failed = 2
}

public enum BillStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    Cash = 1,
    Cheque = 2,
    BankTransfer = 3,
    Card = 4
}

public enum BillingCycle
{
    Monthly = 1,
    HalfYearly = 2,
    Yearly = 3
}

public static class ConnectionTypeExtensions
{
    /** Prefix used inside Order ID and Account ID, mirroring the SRS scheme. */
    public static char OrderPrefix(this ConnectionType type) => type switch
    {
        ConnectionType.DialUp => 'D',
        ConnectionType.Broadband => 'B',
        ConnectionType.Telephone => 'T',
        _ => 'X'
    };

    public static string DisplayName(this ConnectionType type) => type switch
    {
        ConnectionType.DialUp => "Dial-Up",
        ConnectionType.Broadband => "Broadband",
        ConnectionType.Telephone => "Telephone",
        _ => type.ToString()
    };

    /** Dial-Up runs over a landline, so the SRS makes a landline compulsory. */
    public static bool RequiresLandline(this ConnectionType type) =>
        type == ConnectionType.DialUp | type == ConnectionType.Telephone;
}

public static class ConnectionStatusExtensions
{
    public static string DisplayName(this ConnectionStatus status) => status switch
    {
        ConnectionStatus.Pending => "Pending Installation",
        ConnectionStatus.Active => "Active",
        ConnectionStatus.TemporarilyInactive => "Temporarily Inactive",
        ConnectionStatus.PermanentlyInactive => "Permanently Inactive",
        _ => status.ToString()
    };

    /** A temporarily inactive line still bills nothing, per SRS rule V9. */
    public static bool CanBeBilled(this ConnectionStatus status) =>
        status == ConnectionStatus.Active;
}

public static class OrderStatusExtensions
{
    public static string DisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.Draft => "Draft",
        OrderStatus.AwaitingFeasibility => "Awaiting Feasibility Check",
        OrderStatus.FeasibilityPassed => "Feasibility Passed",
        OrderStatus.FeasibilityFailed => "Feasibility Failed",
        OrderStatus.Confirmed => "Confirmed",
        OrderStatus.Installing => "Installation In Progress",
        OrderStatus.Completed => "Completed",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string BadgeClass(this OrderStatus status) => status switch
    {
        OrderStatus.Completed => "badge-success",
        OrderStatus.Confirmed or OrderStatus.Installing => "badge-info",
        OrderStatus.FeasibilityFailed or OrderStatus.Cancelled => "badge-danger",
        OrderStatus.AwaitingFeasibility or OrderStatus.FeasibilityPassed => "badge-warning",
        _ => "badge-neutral"
    };
}

public static class BillStatusExtensions
{
    public static string DisplayName(this BillStatus status) => status switch
    {
        BillStatus.Draft => "Draft",
        BillStatus.Issued => "Issued",
        BillStatus.PartiallyPaid => "Partially Paid",
        BillStatus.Paid => "Paid",
        BillStatus.Overdue => "Overdue",
        BillStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string BadgeClass(this BillStatus status) => status switch
    {
        BillStatus.Paid => "badge-success",
        BillStatus.PartiallyPaid or BillStatus.Issued => "badge-info",
        BillStatus.Overdue => "badge-danger",
        BillStatus.Draft or BillStatus.Cancelled => "badge-neutral",
        _ => "badge-neutral"
    };
}

public static class PaymentMethodExtensions
{
    public static string DisplayName(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Cash",
        PaymentMethod.Cheque => "Cheque",
        PaymentMethod.BankTransfer => "Bank Transfer",
        PaymentMethod.Card => "Card",
        _ => method.ToString()
    };

    public static string DisplayName(this BillingCycle cycle) => cycle switch
    {
        BillingCycle.Monthly => "Monthly",
        BillingCycle.HalfYearly => "Half-Yearly",
        BillingCycle.Yearly => "Yearly",
        _ => cycle.ToString()
    };
}

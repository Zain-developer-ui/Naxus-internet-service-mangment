using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;

namespace NEXUS.Models.ViewModels;

/** One row in the survey / installation queue. */
public sealed record OrderQueueItem(
    int Id,
    string OrderId,
    string CustomerName,
    string AccountId,
    string PlanName,
    string ConnectionTypeName,
    OrderStatus Status,
    string StatusLabel,
    FeasibilityResult CheckResult,
    string CheckLabel,
    double? DistanceKm,
    string CityName,
    decimal QuotedAmount,
    DateTime CreatedAt,
    DateTime? ScheduledFor);

public sealed record OrderQueuePage(
    IReadOnlyList<OrderQueueItem> Orders,
    OrderStatus? Filter)
{
    public int AwaitingCount => Orders.Count(o => o.Status == OrderStatus.AwaitingFeasibility);
    public int ConfirmedCount => Orders.Count(o => o.Status == OrderStatus.Confirmed);
    public int InstallingCount => Orders.Count(o => o.Status == OrderStatus.Installing);
    public bool IsEmpty => Orders.Count == 0;
}

/** Everything the review screen shows about one order. */
public sealed record OrderReview(
    int Id,
    string OrderId,
    string CustomerName,
    string AccountId,
    string Cnic,
    string Phone,
    string PlanName,
    ConnectionType ConnectionType,
    string ConnectionTypeName,
    string BillingCycleName,
    OrderStatus Status,
    string StatusLabel,
    string Address,
    string CityName,
    string? LandlineNumber,
    string? Notes,
    decimal QuotedAmount,
    string? PreferredSlot,
    DateTime? ScheduledFor,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    FeasibilityResult CheckResult,
    string CheckLabel,
    double? DistanceKm,
    bool ExchangeHasCapacity,
    bool LandlineVerified,
    string CheckRemarks,
    DateTime? CheckedAt,
    int? ConnectionId,
    string? ConnectionNumber,
    ConnectionStatus? ConnectionStatus,
    string? ConnectionStatusLabel)
{
    public bool NeedsSurvey => Status == OrderStatus.AwaitingFeasibility;
    public bool CanConfirm => Status == OrderStatus.FeasibilityPassed;
    public bool CanInstall => Status == OrderStatus.Confirmed;
    public bool CanComplete => Status == OrderStatus.Installing;
    public bool CanCancel => Status is not (OrderStatus.Completed or OrderStatus.Cancelled);
    public bool HasConnection => ConnectionId is not null;
    public bool NeedsLandline => FeasibilityRulesPreview.NeedsLandline(ConnectionType);
}

/**
 * The view model cannot reference the service layer, and the "does this type
 * need a landline" rule is shared by both, so it is mirrored here rather than
 * duplicated as an inline expression in the view.
 */
internal static class FeasibilityRulesPreview
{
    public static bool NeedsLandline(ConnectionType type) =>
        type is ConnectionType.DialUp or ConnectionType.Telephone;
}

/** Posted by the survey form. */
public sealed class FeasibilityEntry
{
    [Required]
    public int OrderId { get; set; }

    [Range(0, 100, ErrorMessage = "Distance must be between 0 and 100 km.")]
    [Display(Name = "Distance from exchange (km)")]
    public double? DistanceFromExchangeKm { get; set; }

    [Display(Name = "Exchange has spare capacity")]
    public bool ExchangeHasCapacity { get; set; }

    [Display(Name = "Landline verified")]
    public bool LandlineVerified { get; set; }

    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;
}

/** Posted when confirming a passed order. */
public sealed class ConfirmOrderEntry
{
    [Required]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Pick an installation date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Installation date")]
    public DateTime ScheduledFor { get; set; } = DateTime.UtcNow.Date.AddDays(3);
}

public sealed class CancelOrderEntry
{
    [Required]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Give a reason for cancelling.")]
    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;
}

/** One row in the connection register. */
public sealed record ConnectionRow(
    int Id,
    string ConnectionNumber,
    string CustomerName,
    string AccountId,
    string PlanName,
    string ConnectionTypeName,
    ConnectionStatus Status,
    string StatusLabel,
    DateTime ActivatedOn,
    DateTime? DeactivatedOn,
    string Address,
    string CityName,
    bool IsBillable);

public sealed record ConnectionRegisterTotals(
    int Active,
    int Pending,
    int Suspended,
    int Closed,
    int Billable)
{
    public int Total => Active + Pending + Suspended + Closed;
}

public sealed record ConnectionListPage(
    IReadOnlyList<ConnectionRow> Connections,
    ConnectionStatus? Filter,
    string? Search,
    ConnectionRegisterTotals Totals)
{
    public bool IsEmpty => Connections.Count == 0;
}

public sealed record StatusMove(ConnectionStatus Target, string Label);

public sealed record StatusChangeRow(
    string FromLabel,
    string ToLabel,
    string Reason,
    DateTime ChangedAt);

public sealed record ConnectionDetail(
    int Id,
    string ConnectionNumber,
    int CustomerId,
    string CustomerName,
    string AccountId,
    string Phone,
    string Email,
    string PlanName,
    ConnectionType ConnectionType,
    string ConnectionTypeName,
    ConnectionStatus Status,
    string StatusLabel,
    bool IsBillable,
    DateTime ActivatedOn,
    DateTime? DeactivatedOn,
    string Address,
    string CityName,
    string? LandlineNumber,
    string OrderId,
    int BillCount,
    decimal Outstanding,
    IReadOnlyList<StatusChangeRow> History,
    IReadOnlyList<StatusMove> AllowedMoves);

/** Posted when changing a connection's status. */
public sealed class ConnectionStatusEntry
{
    [Required]
    public int ConnectionId { get; set; }

    [Required]
    public ConnectionStatus Target { get; set; }

    [Required(ErrorMessage = "Give a reason for this change.")]
    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;
}

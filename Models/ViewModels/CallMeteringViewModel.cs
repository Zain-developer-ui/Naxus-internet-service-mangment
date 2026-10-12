using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels;

/**
 * Call metering: the exchange hands over minutes for a line and a period,
 * accounts types them in, and the next bill for that window picks them up.
 */
public sealed class CallMeteringPage
{
    public IReadOnlyList<CallMeterRow> Entries { get; set; } = Array.Empty<CallMeterRow>();

    public CallMeterInput Input { get; set; } = new();

    public IReadOnlyList<CallLineOption> Lines { get; set; } = Array.Empty<CallLineOption>();

    public bool IsEmpty => Entries.Count == 0;
}

public sealed class CallMeterInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick a landline.")]
    [Display(Name = "Connection")]
    public int ConnectionId { get; set; }

    [Display(Name = "Period Start")]
    [DataType(DataType.Date)]
    public DateTime PeriodStart { get; set; } = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

    [Display(Name = "Period End")]
    [DataType(DataType.Date)]
    public DateTime PeriodEnd { get; set; } = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
        .AddMonths(1).AddDays(-1);

    [Range(0, 100000, ErrorMessage = "Local minutes cannot be negative.")]
    [Display(Name = "Local minutes")]
    public decimal LocalMinutes { get; set; }

    [Range(0, 100000, ErrorMessage = "STD minutes cannot be negative.")]
    [Display(Name = "STD minutes")]
    public decimal StdMinutes { get; set; }

    [Range(0, 100000, ErrorMessage = "Mobile minutes cannot be negative.")]
    [Display(Name = "Mobile minutes")]
    public decimal MobileMinutes { get; set; }

    [MaxLength(300)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

public sealed record CallLineOption(
    int Id,
    string ConnectionNumber,
    string AccountId,
    string CustomerName,
    string PlanName,
    string? LandlineNumber,
    string TariffLabel);

public sealed record CallMeterRow(
    int Id,
    int ConnectionId,
    string ConnectionNumber,
    string CustomerName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal LocalMinutes,
    decimal StdMinutes,
    decimal MobileMinutes,
    string TariffLabel,
    bool IsBilled);

using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;

namespace NEXUS.Models.ViewModels;

/**
 * Admin-facing plan catalogue. The public view model (PublicPlan) is a
 * denormalised snapshot for marketing pages; this one carries the editable
 * fields and the per-cycle pricing rows so the edit form can round-trip them.
 */
public sealed class PlanListPage
{
    public IReadOnlyList<PlanRow> Plans { get; set; } = Array.Empty<PlanRow>();

    public ConnectionType? Filter { get; set; }
    public string? Search { get; set; }
    public bool ShowInactive { get; set; }

    public PlanRegisterTotals Totals { get; set; } = new(0, 0, 0, 0);

    public bool IsEmpty => Plans.Count == 0;
}

public sealed record PlanRegisterTotals(int Total, int Active, int Inactive, int Unlimited);

public sealed record PlanRow(
    int Id,
    string Code,
    string Name,
    string Description,
    string ConnectionTypeName,
    string ConnectionTypeToken,
    bool IsUnlimited,
    int? HoursIncluded,
    int? SpeedKbps,
    decimal SecurityDeposit,
    decimal? MonthlyRate,
    int SortOrder,
    bool IsActive,
    int OrderCount);

/**
 * Create and edit share this model. Id is 0 for a new plan.
 */
public sealed class PlanEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    [Display(Name = "Plan Code")]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Plan Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Connection Type")]
    public ConnectionType ConnectionType { get; set; } = ConnectionType.Broadband;

    [Display(Name = "Unlimited plan")]
    public bool IsUnlimited { get; set; }

    [Range(1, 10000, ErrorMessage = "Hours must be between 1 and 10000.")]
    [Display(Name = "Hours Included")]
    public int? HoursIncluded { get; set; }

    [Range(1, 10000000, ErrorMessage = "Speed must be between 1 and 10,000,000 Kbps.")]
    [Display(Name = "Speed (Kbps)")]
    public int? SpeedKbps { get; set; }

    [MaxLength(400)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Sort Order")]
    public int SortOrder { get; set; }

    [Range(0, 100000, ErrorMessage = "Deposit cannot be negative.")]
    [Display(Name = "Security Deposit (USD)")]
    public decimal SecurityDeposit { get; set; }

    /** One row per billing cycle. Always three rows, available or not. */
    public List<PlanPriceRow> Prices { get; set; } = new();

    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "New Plan" : $"Edit {Name}";

    public static PlanEditViewModel Blank() => new()
    {
        Prices = new List<PlanPriceRow>
        {
            new() { Cycle = BillingCycle.Monthly, IsAvailable = true },
            new() { Cycle = BillingCycle.HalfYearly, IsAvailable = false },
            new() { Cycle = BillingCycle.Yearly, IsAvailable = false }
        }
    };
}

public sealed class PlanPriceRow
{
    public BillingCycle Cycle { get; set; }

    [Range(0, 1000000, ErrorMessage = "Rate cannot be negative.")]
    public decimal Amount { get; set; }

    public bool IsAvailable { get; set; }

    public string CycleLabel => Cycle.DisplayName();

    /** Hours-per-month context so the operator can sanity-check a bundle. */
    public string HelpText => Cycle switch
    {
        BillingCycle.Monthly => "Charged every month.",
        BillingCycle.HalfYearly => "Charged once per six months.",
        BillingCycle.Yearly => "Charged once per twelve months.",
        _ => string.Empty
    };
}

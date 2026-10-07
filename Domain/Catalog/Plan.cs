using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Orders;

namespace NEXUS.Domain.Catalog;

/**
 * A service plan exactly as the SRS lists it - hourly bundles and unlimited
 * tiers, priced in USD. PlanPrice holds the billing cycles (monthly,
 * half-yearly, yearly) so the rental can change without touching the plan.
 */
public class Plan : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public ConnectionType ConnectionType { get; set; }

    /** Null for unlimited plans. */
    public int? HoursIncluded { get; set; }

    /** Speed in Kbps for broadband, null for dial-up. */
    public int? SpeedKbps { get; set; }

    public bool IsUnlimited { get; set; }

    [MaxLength(400)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    /** SRS: deposit refundable on permanent disconnection. */
    public decimal SecurityDeposit { get; set; }

    public ICollection<PlanPrice> Prices { get; set; } = new List<PlanPrice>();
    public ICollection<ConnectionOrder> Orders { get; set; } = new List<ConnectionOrder>();

    public string DisplayName => IsUnlimited
        ? $"{Name} (Unlimited)"
        : $"{Name} ({HoursIncluded}h)";
}

public class PlanPrice : AuditableEntity
{
    public int Id { get; set; }

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public BillingCycle Cycle { get; set; }

    /** USD, matching the SRS. Stored as decimal(18,2). */
    public decimal Amount { get; set; }

    /** Some SRS rows have no yearly rate at all. */
    public bool IsAvailable { get; set; } = true;

    public decimal EffectiveMonthlyRate => Cycle switch
    {
        BillingCycle.Monthly => Amount,
        BillingCycle.HalfYearly => Math.Round(Amount / 6m, 2),
        BillingCycle.Yearly => Math.Round(Amount / 12m, 2),
        _ => Amount
    };
}

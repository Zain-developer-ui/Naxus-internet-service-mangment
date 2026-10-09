using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels;

/**
 * Everything the admin console needs to reshape the operating footprint:
 * which cities the ISP serves, and the bulk discount slabs the billing engine
 * applies. Both are small tables with the same list/edit shape as the plan
 * catalogue, so they share this file rather than growing one file per form.
 */

public sealed class CityListPage
{
    public IReadOnlyList<CityRow> Cities { get; set; } = Array.Empty<CityRow>();

    public string? Search { get; set; }

    public CityRegisterTotals Totals { get; set; } = new(0, 0, 0);

    public bool IsEmpty => Cities.Count == 0;
}

public sealed record CityRegisterTotals(int Total, int Serviced, int Customers);

public sealed record CityRow(
    int Id,
    string Name,
    string Region,
    int Code,
    bool IsServiced,
    int CustomerCount,
    int ShopCount);

public sealed class CityEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    [Display(Name = "City Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    [Display(Name = "Region / Province")]
    public string Region { get; set; } = string.Empty;

    /**
     * Embedded in every account ID a customer registers from this city, so it
     * has to stay three digits. Changing it after registrations exist would
     * orphan those IDs, which is why the edit screen locks it once in use.
     */
    [Range(100, 999, ErrorMessage = "City code must be a three digit number.")]
    [Display(Name = "City Code")]
    public int Code { get; set; }

    [Display(Name = "Serving this city")]
    public bool IsServiced { get; set; } = true;

    public int CustomerCount { get; set; }

    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "New City" : $"Edit {Name}";
    public bool CodeLocked => CustomerCount > 0;
}

public sealed class DiscountTierListPage
{
    public IReadOnlyList<DiscountTierRow> Tiers { get; set; } = Array.Empty<DiscountTierRow>();

    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }

    public bool IsEmpty => Tiers.Count == 0;
}

public sealed record DiscountTierRow(
    int Id,
    int MinConnections,
    int? MaxConnections,
    string Label,
    int PercentRate,
    int SortOrder,
    bool IsActive,
    bool Overlaps);

public sealed class DiscountTierEditViewModel
{
    public int Id { get; set; }

    [Range(1, 100000, ErrorMessage = "Minimum connections must be at least 1.")]
    [Display(Name = "From (connections)")]
    public int MinConnections { get; set; }

    /**
     * Null means "and above" - an open ended top slab. The form uses a tick
     * box because an empty number input and a deliberately blank one look
     * identical on the wire.
     */
    [Range(2, 100000, ErrorMessage = "Maximum must be greater than the minimum.")]
    [Display(Name = "To (connections)")]
    public int? MaxConnections { get; set; }

    [Display(Name = "Open ended (and above)")]
    public bool IsOpenEnded { get; set; }

    [Range(0.01, 1.0, ErrorMessage = "Discount must be between 1% and 100%.")]
    [Display(Name = "Discount Rate")]
    public decimal Rate { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Sort Order")]
    public int SortOrder { get; set; }

    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "New Discount Tier" : "Edit Discount Tier";

    /** Slab text for the list column, driven by whichever form the operator chose. */
    public string Label => IsOpenEnded || MaxConnections is null
        ? $"{MinConnections}+"
        : $"{MinConnections}-{MaxConnections}";

    /** The editor takes percent, the database stores a 0-1 fraction. */
    public int PercentRate
    {
        get => (int)Math.Round(Rate * 100);
        set => Rate = Math.Clamp(value, 1, 100) / 100m;
    }
}

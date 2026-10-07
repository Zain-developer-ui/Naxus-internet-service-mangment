namespace NEXUS.Models.ViewModels;

/** One purchasable plan as the public site shows it. */
public sealed record PublicPlan(
    int Id,
    string Code,
    string Name,
    string ConnectionTypeName,
    string ConnectionTypeToken,
    bool IsUnlimited,
    int? HoursIncluded,
    int? SpeedKbps,
    decimal SecurityDeposit,
    decimal MonthlyRate,
    decimal? YearlyRate,
    string Badge,
    string Description)
{
    /** Broadband cards lead with the speed; everything else leads with hours. */
    public string Headline => SpeedKbps is int kbps
        ? $"{kbps} Kbps"
        : HoursIncluded is int hours
            ? $"{hours} hours"
            : "Unlimited";
}

/** The /Plans page: a filterable catalogue. */
public sealed record PlanCatalogue(
    IReadOnlyList<PublicPlan> Plans,
    IReadOnlyList<string> Types,
    string? ActiveType)
{
    public bool IsFiltered => !string.IsNullOrWhiteSpace(ActiveType);
}

/** The home page preview: a short, fixed selection rather than the full list. */
public sealed record FeaturedPlans(IReadOnlyList<PublicPlan> Plans)
{
    public bool HasAny => Plans.Count > 0;
}

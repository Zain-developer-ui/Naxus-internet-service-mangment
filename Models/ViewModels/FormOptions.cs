namespace NEXUS.Models.ViewModels;

/** A serviceable city, shown in the city picker. */
public sealed record CityOption(int Id, string Name, string Region);

/**
 * Pre-rendered plan data for the plan picker. The monthly rate is resolved
 * here so the form can show what the customer actually pays per month
 * regardless of which billing cycle they choose.
 */
public sealed record PlanOption(
    int Id,
    string Name,
    string ConnectionType,
    bool IsUnlimited,
    int? HoursIncluded,
    int? SpeedKbps,
    decimal SecurityDeposit,
    decimal MonthlyRate,
    string Badge)
{
    public decimal YearlyRate => Math.Round(MonthlyRate * 12m, 2);
}

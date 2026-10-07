using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Catalog;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Catalog;

/**
 * Read-only access to the plan catalogue for the public site. The order form
 * builds its own filtered view through RegistrationService, but the marketing
 * pages only need to list and describe what is on offer.
 */
public interface IPlanCatalogService
{
    Task<Result<PlanCatalogue>> GetCatalogueAsync(string? type, CancellationToken ct = default);
    Task<Result<PublicPlan>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PublicPlan>>> FeatureAsync(int take, CancellationToken ct = default);
}

public sealed class PlanCatalogService : IPlanCatalogService
{
    private readonly NexusDbContext _db;

    public PlanCatalogService(NexusDbContext db) => _db = db;

    public async Task<Result<PlanCatalogue>> GetCatalogueAsync(string? type, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        if (all.Count == 0)
            return Result<PlanCatalogue>.Fail(ErrorKind.Failure,
                "No plans are configured yet. Contact the administrator.");

        // The filter is driven by the query string, so an unknown value must
        // fall back to the full list rather than showing an empty page.
        var active = ResolveType(type, all);
        var plans = active is null
            ? all
            : all.Where(p => p.ConnectionTypeToken == active).ToList();

        var types = all
            .Select(p => p.ConnectionTypeToken)
            .Distinct()
            .ToList();

        return Result<PlanCatalogue>.Ok(new PlanCatalogue(plans, types, active));
    }

    public async Task<Result<PublicPlan>> GetAsync(int id, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        var plan = all.FirstOrDefault(p => p.Id == id);

        return plan is null
            ? Result<PublicPlan>.NotFound($"No plan exists with id {id}.")
            : Result<PublicPlan>.Ok(plan);
    }

    public async Task<Result<IReadOnlyList<PublicPlan>>> FeatureAsync(int take, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);

        /**
         * The home page preview is meant to show a spread of what we sell,
         * not four broadband tiers. One plan per connection type first, then
         * whatever is left in catalogue order to fill the row.
         */
        var picked = new List<PublicPlan>();
        foreach (var token in all.Select(p => p.ConnectionTypeToken).Distinct())
        {
            var first = all.FirstOrDefault(p => p.ConnectionTypeToken == token);
            if (first is not null) picked.Add(first);
        }

        foreach (var plan in all)
        {
            if (picked.Count >= take) break;
            if (picked.Any(p => p.Id == plan.Id)) continue;
            picked.Add(plan);
        }

        return Result<IReadOnlyList<PublicPlan>>.Ok(picked.Take(take).ToList());
    }

    private async Task<List<PublicPlan>> LoadAsync(CancellationToken ct)
    {
        var plans = await _db.Plans
            .AsNoTracking()
            .Include(p => p.Prices)
            .Where(p => p.IsActive)
            .OrderBy(p => p.ConnectionType)
            .ThenBy(p => p.SortOrder)
            .ToListAsync(ct);

        return plans.Select(ToPublic).ToList();
    }

    private static string? ResolveType(string? type, List<PublicPlan> plans)
    {
        if (string.IsNullOrWhiteSpace(type)) return null;

        var match = plans.FirstOrDefault(p =>
            string.Equals(p.ConnectionTypeToken, type, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.ConnectionTypeName, type, StringComparison.OrdinalIgnoreCase));

        return match?.ConnectionTypeToken;
    }

    private static PublicPlan ToPublic(Plan plan)
    {
        var monthly = plan.Prices.FirstOrDefault(p => p.Cycle == BillingCycle.Monthly && p.IsAvailable);
        var rate = monthly?.Amount
                   ?? plan.Prices.Where(p => p.IsAvailable)
                                 .Select(p => p.EffectiveMonthlyRate)
                                 .DefaultIfEmpty(0m)
                                 .Min();

        var yearly = plan.Prices.FirstOrDefault(p => p.Cycle == BillingCycle.Yearly && p.IsAvailable);

        var badge = plan.IsUnlimited
            ? "Unlimited"
            : plan.HoursIncluded is int hours
                ? $"{hours} hours"
                : plan.ConnectionType.DisplayName();

        return new PublicPlan(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.ConnectionType.DisplayName(),
            plan.ConnectionType.ToString(),
            plan.IsUnlimited,
            plan.HoursIncluded,
            plan.SpeedKbps,
            plan.SecurityDeposit,
            Math.Round(rate, 2),
            yearly is null ? null : Math.Round(yearly.Amount, 2),
            badge,
            plan.Description);
    }
}

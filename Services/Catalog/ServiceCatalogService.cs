using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Catalog;

/**
 * The marketing service pages. Rather than a hardcoded list, each connection
 * type we actually sell becomes a service entry, described from the plans in
 * the catalogue - so adding a plan in admin updates the public site too.
 */
public interface IServiceCatalogService
{
    Task<Result<IReadOnlyList<ServiceViewModel>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<ServiceViewModel>> GetAsync(int id, CancellationToken ct = default);
}

public sealed class ServiceCatalogService : IServiceCatalogService
{
    private readonly NexusDbContext _db;

    public ServiceCatalogService(NexusDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ServiceViewModel>>> GetAllAsync(CancellationToken ct = default)
    {
        var plans = await _db.Plans
            .AsNoTracking()
            .Include(p => p.Prices)
            .Where(p => p.IsActive)
            .OrderBy(p => p.ConnectionType)
            .ThenBy(p => p.SortOrder)
            .ToListAsync(ct);

        if (plans.Count == 0)
            return Result<IReadOnlyList<ServiceViewModel>>.Fail(ErrorKind.Failure,
                "No services are published yet.");

        var services = plans
            .GroupBy(p => p.ConnectionType)
            .Select((group, index) => ToService(index + 1, group.Key, group.ToList()))
            .ToList();

        return Result<IReadOnlyList<ServiceViewModel>>.Ok(services);
    }

    public async Task<Result<ServiceViewModel>> GetAsync(int id, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        if (!all.IsSuccess) return Result<ServiceViewModel>.Fail(all.Kind, all.Message!);

        var service = all.Value!.FirstOrDefault(s => s.Id == id);

        return service is null
            ? Result<ServiceViewModel>.NotFound($"No service exists with id {id}.")
            : Result<ServiceViewModel>.Ok(service);
    }

    private static ServiceViewModel ToService(int id, ConnectionType type, List<Domain.Catalog.Plan> plans)
    {
        var priced = plans.Where(p => p.Prices.Any(x => x.IsAvailable)).ToList();
        var pool = priced.Count > 0 ? priced : plans;

        var speeds = pool.Where(p => p.SpeedKbps is not null)
                         .Select(p => p.SpeedKbps!.Value)
                         .ToList();

        var lowest = pool.Select(p => p.Prices
                            .Where(x => x.IsAvailable)
                            .Select(x => x.Amount)
                            .DefaultIfEmpty(p.SecurityDeposit)
                            .Min())
                         .DefaultIfEmpty(0m)
                         .Min();

        var availability = plans.Any(p => p.IsUnlimited) ? "Nationwide" : "Selected areas";

        var image = type switch
        {
            ConnectionType.DialUp => "/images/dial-up.jpg",
            ConnectionType.Telephone => "/images/telephone.jpg",
            _ => "/images/broadband.jpg"
        };

        return new ServiceViewModel
        {
            Id = id,
            Name = type.DisplayName(),
            Category = "Internet",
            IconClass = type switch
            {
                ConnectionType.DialUp => "fa-solid fa-phone",
                ConnectionType.Telephone => "fa-solid fa-phone-volume",
                _ => "fa-solid fa-wifi"
            },
            ImageUrl = image,
            ShortDescription = $"NEXUS {type.DisplayName()} packages from {lowest:C} per cycle.",
            FullDescription = BuildDescription(type, pool.Count, lowest),
            SpeedRange = speeds.Count == 0
                ? "Varies by package"
                : $"{speeds.Min():N0}-{speeds.Max():N0} Kbps",
            Availability = availability,
            Features = pool.SelectMany(p => new[] { p.Name }).Distinct().Take(6).ToList(),
            Benefits = type switch
            {
                ConnectionType.DialUp => new List<string>
                {
                    "Works over an existing landline",
                    "No router required",
                    "Lowest monthly cost"
                },
                ConnectionType.Telephone => new List<string>
                {
                    "Line rental and service combined",
                    "Free local calls on some packages",
                    "Priority fault handling"
                },
                _ => new List<string>
                {
                    "Always-on connection",
                    "Free router on annual packages",
                    "24/7 technical support"
                }
            }
        };
    }

    private static string BuildDescription(ConnectionType type, int planCount, decimal from) =>
        $"{type.DisplayName()} service with {planCount} " +
        $"{(planCount == 1 ? "package" : "packages")} currently offered, starting at {from:C}. " +
        type switch
        {
            ConnectionType.DialUp =>
                "Dial-up runs over a standard telephone line, so no new cabling is needed and the line is billed alongside your existing rental.",
            ConnectionType.Telephone =>
                "Telephone service combines line rental with call packages, installed and maintained by the same crew that handles your internet connection.",
            _ =>
                "Broadband packages are always-on and unmetered within the hours included, with the router supplied by NEXUS and maintained for the life of the contract."
        };
}

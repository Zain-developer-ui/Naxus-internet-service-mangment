namespace NEXUS.Common.Constants;

/**
 * Bulk / corporate discount slabs from the SRS. The upper bound of the top
 * slab is open ended, which is why TierFor returns null for anything under
 * the minimum rather than a zero percent tier - "no discount" and "0% tier"
 * mean different things when the bill is being explained to a customer.
 */
public sealed record BulkDiscountTier(int MinConnections, int? MaxConnections, decimal Rate)
{
    public string Label => MaxConnections is null
        ? $"{MinConnections}+ connections"
        : $"{MinConnections}-{MaxConnections} connections";

    public bool Covers(int count) =>
        count >= MinConnections && (MaxConnections is null || count < MaxConnections.Value);
}

public static class BulkDiscountTiers
{
    public static readonly IReadOnlyList<BulkDiscountTier> All = new[]
    {
        new BulkDiscountTier(10, 15, 0.25m),
        new BulkDiscountTier(15, 25, 0.50m),
        new BulkDiscountTier(25, 50, 0.75m),
        new BulkDiscountTier(50, null, 1.00m)
    };

    public static readonly int MinimumConnections = 10;

    public static BulkDiscountTier? TierFor(int connectionCount) =>
        All.FirstOrDefault(t => t.Covers(connectionCount));

    public static decimal RateFor(int connectionCount) =>
        TierFor(connectionCount)?.Rate ?? 0m;
}

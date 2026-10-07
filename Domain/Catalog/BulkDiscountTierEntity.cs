using NEXUS.Common.Constants;

namespace NEXUS.Domain.Catalog;

/**
 * The SRS bulk discount slabs live in the database rather than only in code,
 * so an admin can retune them without a deploy. BulkDiscountTiers holds the
 * defaults used for seeding and as the fallback.
 */
public class BulkDiscountTierEntity : AuditableEntity
{
    public int Id { get; set; }

    public int MinConnections { get; set; }
    public int? MaxConnections { get; set; }

    /** 0.25 for 25%, stored as a decimal so the bill maths stays exact. */
    public decimal Rate { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool Covers(int count) =>
        count >= MinConnections && (MaxConnections is null || count < MaxConnections.Value);

    public string Label => MaxConnections is null
        ? $"{MinConnections}+"
        : $"{MinConnections}-{MaxConnections}";

    public int PercentRate => (int)Math.Round(Rate * 100);
}

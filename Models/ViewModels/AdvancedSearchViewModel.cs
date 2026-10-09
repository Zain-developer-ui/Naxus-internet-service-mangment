using NEXUS.Common.Constants;

namespace NEXUS.Models.ViewModels;

/**
 * The five filters the SRS names for the advanced search: the unique id, the
 * name the order or connection is held under, the connection type, a date or
 * period, and the contact number given at application time.
 *
 * Every field is optional. An empty query is not a search - it returns nothing
 * rather than the whole book, which is what a clerk landing on the screen means
 * by having typed nothing.
 */
public sealed class AdvancedSearchQuery
{
    /** Matches an order id, an account id or a connection number. */
    public string? Id { get; set; }

    public string? Name { get; set; }

    public ConnectionType? Type { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public string? Contact { get; set; }

    public bool HasAnyFilter =>
        !string.IsNullOrWhiteSpace(Id) ||
        !string.IsNullOrWhiteSpace(Name) ||
        Type is not null ||
        From is not null ||
        To is not null ||
        !string.IsNullOrWhiteSpace(Contact);

    /** Echoed back on the form so the URL stays shareable. */
    public string FromValue => From?.ToString("yyyy-MM-dd") ?? string.Empty;
    public string ToValue => To?.ToString("yyyy-MM-dd") ?? string.Empty;

    public string PeriodLabel
    {
        get
        {
            if (From is null && To is null) return "Any date";
            if (From is not null && To is not null) return $"{From:dd MMM yyyy} - {To:dd MMM yyyy}";
            if (From is not null) return $"From {From:dd MMM yyyy}";
            return $"Up to {To:dd MMM yyyy}";
        }
    }
}

public sealed class AdvancedSearchResults
{
    public AdvancedSearchQuery Query { get; init; } = new();

    /** Orders, filtered on the date they were placed. */
    public IReadOnlyList<SearchOrderRow> Orders { get; init; } = Array.Empty<SearchOrderRow>();

    /** Live lines, filtered on the date they were activated. */
    public IReadOnlyList<SearchConnectionRow> Connections { get; init; } = Array.Empty<SearchConnectionRow>();

    public bool Searched { get; init; }
    public int TotalHits => Orders.Count + Connections.Count;
}

public sealed record SearchConnectionRow(
    int ConnectionId,
    string ConnectionNumber,
    string AccountId,
    string CustomerName,
    string ConnectionType,
    string Plan,
    string Status,
    string StatusToken,
    DateTime ActivatedOn,
    string City);

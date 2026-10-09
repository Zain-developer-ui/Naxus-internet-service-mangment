namespace NEXUS.Models.ViewModels;

/**
 * A labelled share of a total, used where a doughnut chart would otherwise be
 * the third pie on a page. Values arrive as decimals from the chart feeders, so
 * the row keeps them that way and lets the partial trim to whole numbers.
 */
public sealed record BreakdownRow(string Label, decimal Value)
{
    public BreakdownRow(string label, int value) : this(label, (decimal)value) { }
}

public sealed record BreakdownPanel(IReadOnlyList<BreakdownRow> Rows);

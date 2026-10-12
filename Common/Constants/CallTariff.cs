namespace NEXUS.Common.Constants;

/**
 * Landline call rates from the SRS. The document lists them per plan shape
 * rather than per plan row, so they live here as named tariffs and the
 * metering screen picks one for the line being billed.
 *
 * Amounts are USD per minute, exactly as printed in the SRS.
 */
public static class CallTariff
{
    public sealed record Rate(decimal Local, decimal Std, decimal Mobile, string Label);

    /** Local Plan - unlimited rental with a flat per-minute call rate. */
    public static readonly Rate LocalUnlimited =
        new(0.55m, 0m, 0m, "Local - unlimited rental");

    /** Local Plan - monthly rental. */
    public static readonly Rate LocalMonthly =
        new(0.75m, 0m, 0m, "Local - monthly");

    /** STD Plan - monthly. Local calls are cheaper than STD. */
    public static readonly Rate StdMonthly =
        new(0.70m, 2.25m, 1.00m, "STD - monthly");

    /** STD Plan - half-yearly. */
    public static readonly Rate StdHalfYearly =
        new(0.60m, 2.00m, 1.15m, "STD - half-yearly");

    /** STD Plan - yearly. */
    public static readonly Rate StdYearly =
        new(0.55m, 1.75m, 1.25m, "STD - yearly");

    public static readonly IReadOnlyList<Rate> All = new[]
    {
        LocalUnlimited, LocalMonthly, StdMonthly, StdHalfYearly, StdYearly
    };

    public static Rate ByLabel(string? label) =>
        All.FirstOrDefault(r => r.Label == label) ?? StdMonthly;
}

namespace NEXUS.Common.Constants;

/**
 * SRS section on billing. Kept here so the UI, the bill engine and the tests
 * all read the same number instead of each keeping its own copy.
 */
public static class TaxConstants
{
    public const decimal ServiceTaxRate = 0.1224m;
    public const string ServiceTaxLabel = "Service Tax (12.24%)";

    public static decimal Compute(decimal taxable) =>
        Math.Round(taxable * ServiceTaxRate, 2, MidpointRounding.AwayFromZero);
}

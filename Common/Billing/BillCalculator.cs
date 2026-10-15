using NEXUS.Common.Constants;

namespace NEXUS.Common.Billing;

/**
 * One computed charge before it becomes a BillLine. Kept separate from the
 * entity so the arithmetic can be reasoned about - and tested - without a
 * database or a DbContext in the way.
 */
public sealed record ChargeLine(
    string Description,
    string ChargeType,
    decimal Quantity,
    decimal UnitAmount)
{
    public decimal LineAmount => Math.Round(Quantity * UnitAmount, 2, MidpointRounding.AwayFromZero);
}

/**
 * The full arithmetic for one bill, from the lines through the bulk discount
 * to the service tax. The order matters and comes straight off the SRS:
 *
 *   subtotal  = sum of lines
 *   discount  = discountable lines only * tier rate
 *   taxable   = subtotal - discount
 *   tax       = taxable * 12.24%
 *   total     = taxable + tax
 *
 * FR-99 is the reason the discount is not simply subtotal * rate. The SRS
 * grants the bulk/corporate rate on the advance and the security deposit - the
 * one-off money a customer puts down - and says nothing about the recurring
 * rental. Discounting the rental too would quietly under-bill every month, so
 * only the lines in DiscountableChargeTypes count.
 */
public sealed record BillBreakdown(
    IReadOnlyList<ChargeLine> Lines,
    int ConnectionCount,
    BulkDiscountTier? Tier)
{
    public decimal SubTotal =>
        Math.Round(Lines.Sum(l => l.LineAmount), 2, MidpointRounding.AwayFromZero);

    /** The part of the subtotal the tier applies to (FR-99). */
    public decimal DiscountableSubTotal =>
        Math.Round(Lines.Where(l => ChargeTypes.IsDiscountable(l.ChargeType))
                        .Sum(l => l.LineAmount), 2, MidpointRounding.AwayFromZero);

    public decimal DiscountRate => Tier?.Rate ?? 0m;

    public decimal DiscountAmount =>
        Math.Round(DiscountableSubTotal * DiscountRate, 2, MidpointRounding.AwayFromZero);

    public decimal TaxableAmount => SubTotal - DiscountAmount;

    /**
     * Tax is never negative. A period whose credits outrun its charges is a
     * zero bill, not a refund of tax the customer never paid.
     */
    public decimal ServiceTaxAmount => TaxConstants.Compute(Math.Max(0m, TaxableAmount));

    /**
     * A bill is never a credit note. The advance is applied as far as the
     * period's own charges allow (`ApplyAdvanceAsync`), but a fresh charge
     * added after the advance was worked out - the replacement router on
     * INV-2026-00001 - can still push the arithmetic below zero. Clamping here
     * keeps the money owed at zero and leaves the surplus on the order, where
     * the next bill picks it up.
     */
    public decimal TotalAmount => Math.Max(0m, TaxableAmount + ServiceTaxAmount);

    public bool HasDiscount => DiscountAmount > 0m;
}

/**
 * Rental pricing has two shapes in the SRS and they must not be mixed up:
 * an hourly bundle is charged once per billing period at the plan rate, while
 * a cycle-based price is the amount for that whole cycle (a yearly rate is not
 * twelve times the monthly one - it is its own number).
 *
 * Landline call charges are billed as part of a telephone/Dial-Up line's bill
 * rather than tracked here; they arrive as plain ChargeLines with metered
 * quantities.
 */
public static class BillCalculator
{
    /** Years are 365 days for proration so the figure does not move on leap years. */
    private const decimal DaysPerYear = 365m;

    public static BillBreakdown Compute(IEnumerable<ChargeLine> lines,
                                        int connectionCount)
    {
        var materialised = lines.ToList();
        var tier = BulkDiscountTiers.TierFor(connectionCount);

        return new BillBreakdown(materialised, connectionCount, tier);
    }

    /**
     * The recurring rental for one connection over the period being billed.
     * A cycle that spans more than one month multiplies the monthly rate, so a
     * half-yearly bill for a $50 plan is six months of rental in one go.
     */
    public static ChargeLine RentalLine(string planName, decimal monthlyRate,
                                        BillingCycle cycle, DateTime periodStart,
                                        DateTime periodEnd)
    {
        var months = MonthsBetween(periodStart, periodEnd);

        return new ChargeLine(
            $"{planName} - rental ({MonthsLabel(months)})",
            ChargeTypes.Rental,
            months,
            Math.Round(monthlyRate, 2));
    }

    /**
     * Meters are billed per whole unit. Usage above the included hours is
     * charged at the plan's overflow rate; plans that include everything pass
     * a null rate and simply produce no line.
     */
    public static ChargeLine? UsageLine(int includedHours, decimal billedHours,
                                        decimal perHourRate)
    {
        var overflow = billedHours - includedHours;
        if (overflow <= 0m || perHourRate <= 0m) return null;

        return new ChargeLine(
            $"Usage above {includedHours}h included",
            ChargeTypes.Usage,
            Math.Round(overflow, 2),
            Math.Round(perHourRate, 2));
    }

    /**
     * Landline calls are priced per minute in the SRS, with local and STD
     * tariffs kept apart because the customer-facing bill must show both.
     */
    public static ChargeLine CallLine(string label, decimal minutes, decimal perMinuteRate) =>
        new(label, ChargeTypes.Calls, Math.Round(minutes, 2), Math.Round(perMinuteRate, 2));

    /** A one-off charge such as installation or a new connection fee. */
    public static ChargeLine OneOffLine(string label, decimal amount, string chargeType) =>
        new(label, chargeType, 1m, Math.Round(amount, 2));

    /**
     * Prorates a monthly rate for a partial period. Used when a connection is
     * activated or closed part-way through the billing month.
     */
    public static decimal ProrateMonthly(decimal monthlyRate, DateTime from, DateTime to)
    {
        if (to <= from) return 0m;

        var days = (decimal)(to.Date - from.Date).TotalDays;
        return Math.Round(monthlyRate * days / (DaysPerYear / 12m), 2, MidpointRounding.AwayFromZero);
    }

    private static decimal MonthsBetween(DateTime start, DateTime end)
    {
        var months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
        if (months <= 0) months = 1;

        // A period that starts mid-month and ends mid-month is still one month
        // of service; the day component only matters for proration.
        return months;
    }

    private static string MonthsLabel(decimal months) =>
        months == 1m ? "1 month" : $"{months:0.##} months";
}

/**
 * ChargeType is a free-text column on BillLine, so the values are centralised
 * here rather than typed as literals at each call site and drifting apart.
 */
public static class ChargeTypes
{
    public const string Rental = "Rental";
    public const string Usage = "Usage";
    public const string Calls = "Call Charges";
    public const string Installation = "Installation";
    public const string SecurityDeposit = "Security Deposit";
    public const string Equipment = "Equipment";
    public const string Adjustment = "Adjustment";
    public const string AdvancePayment = "Advance Payment";
    public const string Replacement = "Replacement";

    /**
     * FR-99: the bulk/corporate discount is granted on the advance and the
     * security deposit. Installation sits with them as part of the same
     * one-off connection cost; the recurring rental and any usage are never
     * discounted.
     */
    private static readonly HashSet<string> Discountable = new(StringComparer.OrdinalIgnoreCase)
    {
        AdvancePayment,
        SecurityDeposit,
        Installation
    };

    public static bool IsDiscountable(string? chargeType) =>
        chargeType is not null && Discountable.Contains(chargeType);
}

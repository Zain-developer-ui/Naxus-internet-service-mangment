using NEXUS.Common.Constants;

namespace NEXUS.Services.Feasibility;

/**
 * The rules the survey applies. The SRS says a check is needed but never gives
 * the numbers, so they live here as constants rather than being scattered
 * through the service - if the business settles on different limits, this is
 * the only place to change.
 */
public static class FeasibilityRules
{
    /** A line beyond this distance cannot be served without new plant. */
    public const double MaxDistanceKm = 5.0;

    /**
     * Distance is graded rather than pass/fail: past the soft limit the survey
     * still passes, but the technician is warned that a booster may be needed.
     */
    public const double SoftDistanceKm = 3.5;

    public const string DistanceTooFar =
        "The installation address is more than 5 km from the nearest exchange.";
    public const string NoCapacity =
        "The local exchange has no spare capacity for a new line.";
    public const string LandlineMissing =
        "A dial-up or telephone line requires an existing landline at the address.";
    public const string LandlineNotDocumented =
        "This order needs a landline number before the survey can pass.";

    /** Dial-Up and Telephone ride on an existing landline (SRS rule V1). */
    public static bool NeedsLandline(ConnectionType type) =>
        type is ConnectionType.DialUp or ConnectionType.Telephone;

    public static bool ExceedsHardLimit(double? distanceKm) =>
        distanceKm is double d && d > MaxDistanceKm;

    public static bool ExceedsSoftLimit(double? distanceKm) =>
        distanceKm is double d && d > SoftDistanceKm && d <= MaxDistanceKm;
}

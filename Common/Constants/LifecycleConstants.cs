namespace NEXUS.Common.Constants;

/**
 * The SRS makes a postpaid connection's status follow its bill - "the bill will
 * be generated based on which the status of the connection depends". The two
 * windows below are that policy. They live here so the preview screen, the
 * sweep and any future scheduler all read the same numbers.
 */
public static class LifecycleConstants
{
    /** Days past the due date before a live line is suspended. */
    public const int SuspensionGraceDays = 15;

    /** Days past the due date before the line is closed for good. */
    public const int PermanentClosureDays = 45;

    public static string SuspensionReason(int daysOverdue) =>
        $"Payment outstanding for {daysOverdue} days. Line suspended under the postpaid policy.";

    public static string ClosureReason(int daysOverdue) =>
        $"Payment outstanding for {daysOverdue} days. Line closed after the suspension window lapsed.";
}

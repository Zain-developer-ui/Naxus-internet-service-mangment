namespace NEXUS.Common.Constants;

/**
 * The five roles the SRS describes. String constants rather than an enum so
 * they drop straight into [Authorize(Roles = ...)] and into the identity
 * claims cookie without a conversion step.
 */
public static class NexusRoles
{
    public const string Admin = "Admin";
    public const string Accounts = "Accounts";
    public const string Technical = "Technical";
    public const string Retail = "Retail";
    public const string Customer = "Customer";

    public const string StaffRoles = Admin + "," + Accounts + "," + Technical + "," + Retail;

    public static readonly IReadOnlyList<string> All = new[]
    {
        Admin, Accounts, Technical, Retail, Customer
    };

    public static string DisplayName(string role) => role switch
    {
        Admin => "Administrator",
        Accounts => "Accounts Department",
        Technical => "Technical Staff",
        Retail => "Retail Outlet",
        Customer => "Customer",
        _ => role
    };
}

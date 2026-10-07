using System.Security.Claims;
using NEXUS.Common.Constants;

namespace NEXUS.Common.Extensions;

/**
 * Reading the signed-in identity off HttpContext. Views and controllers both
 * go through this so the claim names never leak into view code.
 */
public static class ClaimsPrincipalExtensions
{
    public static int? UserId(this ClaimsPrincipal? user) =>
        ParseInt(user?.FindFirst(NexusClaims.UserId)?.Value);

    public static int? CustomerId(this ClaimsPrincipal? user) =>
        ParseInt(user?.FindFirst(NexusClaims.CustomerId)?.Value);

    public static int? EmployeeId(this ClaimsPrincipal? user) =>
        ParseInt(user?.FindFirst(NexusClaims.EmployeeId)?.Value);

    public static int? ShopId(this ClaimsPrincipal? user) =>
        ParseInt(user?.FindFirst(NexusClaims.ShopId)?.Value);

    public static string? AccountId(this ClaimsPrincipal? user) =>
        user?.FindFirst(NexusClaims.AccountId)?.Value;

    public static string DisplayName(this ClaimsPrincipal? user) =>
        user?.FindFirst(NexusClaims.FullName)?.Value
        ?? user?.Identity?.Name
        ?? "Guest";

    public static string RoleName(this ClaimsPrincipal? user) =>
        user?.FindFirst(NexusClaims.Role)?.Value ?? NexusRoles.Customer;

    public static bool IsStaff(this ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true &&
        user.RoleName() != NexusRoles.Customer;

    public static bool MustChangePassword(this ClaimsPrincipal? user) =>
        string.Equals(user?.FindFirst(NexusClaims.MustChangePassword)?.Value,
                      "true", StringComparison.OrdinalIgnoreCase);

    public static string Initials(this ClaimsPrincipal? user)
    {
        var name = user.DisplayName();
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";

        return string.Concat(parts.Take(2).Select(w => char.ToUpperInvariant(w[0])));
    }

    private static int? ParseInt(string? raw) =>
        int.TryParse(raw, out var value) ? value : null;
}

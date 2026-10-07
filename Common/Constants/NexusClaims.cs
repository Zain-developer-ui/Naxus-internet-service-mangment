namespace NEXUS.Common.Constants;

/**
 * Claim types written into the auth cookie. Named here so the login action and
 * every reader agree on the same strings instead of repeating literals.
 */
public static class NexusClaims
{
    public const string UserId = "nx:uid";
    public const string AccountId = "nx:account";
    public const string FullName = "nx:name";
    public const string Role = "nx:role";
    public const string EmployeeId = "nx:eid";
    public const string CustomerId = "nx:cid";
    public const string ShopId = "nx:shop";
    public const string MustChangePassword = "nx:pwdchange";
}

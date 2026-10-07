using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NEXUS.Common.Constants;

namespace NEXUS.Services.Authentication;

public interface ISignInService
{
    Task SignInAsync(HttpContext context, AuthenticatedUser user, bool persistent);
    Task SignOutAsync(HttpContext context);
}

public sealed class SignInService : ISignInService
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public async Task SignInAsync(HttpContext context, AuthenticatedUser user, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(NexusClaims.UserId, user.User.Id.ToString()),
            new(NexusClaims.AccountId, user.User.AccountId),
            new(NexusClaims.FullName, user.User.FullName),
            new(NexusClaims.Role, user.Role),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.EmployeeId is int employeeId)
            claims.Add(new Claim(NexusClaims.EmployeeId, employeeId.ToString()));

        if (user.CustomerId is int customerId)
            claims.Add(new Claim(NexusClaims.CustomerId, customerId.ToString()));

        if (user.ShopId is int shopId)
            claims.Add(new Claim(NexusClaims.ShopId, shopId.ToString()));

        if (user.User.MustChangePassword)
            claims.Add(new Claim(NexusClaims.MustChangePassword, "true"));

        var identity = new ClaimsIdentity(claims, Scheme);

        await context.SignInAsync(Scheme, new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = persistent,
            ExpiresUtc = persistent
                ? DateTimeOffset.UtcNow.AddDays(7)
                : null
        });
    }

    public Task SignOutAsync(HttpContext context) => context.SignOutAsync(Scheme);
}

namespace NEXUS.Services.Security;

/**
 * Adds the response headers a browser needs to lock a page down, plus a policy
 * that names every source the site actually loads from.
 *
 * The CSP is deliberately not 'unsafe-inline' for scripts. Markup in this app
 * still uses inline style attributes in a few older views, so style-src keeps
 * 'unsafe-inline' for now - that is a real gap, not an oversight, and the
 * remaining inline blocks are tracked for cleanup.
 */
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // Only worth sending over TLS - on the dev HTTP port the browser
            // would ignore it anyway and the mixed content warnings are noise.
            if (context.Request.IsHttps)
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            headers["Content-Security-Policy"] = BuildPolicy(context);

            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string BuildPolicy(HttpContext context)
    {
        // Razor emits no inline script of its own, but the anti-forgery helper
        // and the tariff preview on /Accounts/Calls are inline blocks, hence
        // the nonce on those. Fonts come from the system stack plus a CDN copy.
        var nonce = context.Items["csp-nonce"] as string;

        var scriptSrc = nonce is null
            ? "'self'"
            : $"'self' 'nonce-{nonce}'";

        return string.Join("; ",
            "default-src 'self'",
            $"script-src {scriptSrc}",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data:",
            "font-src 'self' data:",
            "connect-src 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "object-src 'none'");
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseNexusSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}

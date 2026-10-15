using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NEXUS.Common.Abstractions;
using NEXUS.Data;
using NEXUS.Data.Seed;
using NEXUS.Services.Authentication;
using NEXUS.Services.Billing;
using NEXUS.Services.Catalog;
using NEXUS.Services.Email;
using NEXUS.Services.Feedback;
using NEXUS.Services.Notifications;
using NEXUS.Services.Orders;
using NEXUS.Services.Reporting;
using NEXUS.Services.CustomerPortal;
using NEXUS.Services.Accounts;
using NEXUS.Services.Dashboards;
using NEXUS.Services.Feasibility;
using NEXUS.Services.Generation;
using NEXUS.Services.Profile;
using NEXUS.Services.Registration;
using NEXUS.Services.Retail;
using NEXUS.Services.Search;
using NEXUS.Services.Security;
using NEXUS.Services.Settings;
using NEXUS.Services.Inventory;
using NEXUS.Services.Lifecycle;
using NEXUS.Services.Organisation;
using NEXUS.Services.Procurement;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("Logs/nexus-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

builder.Host.UseSerilog();

/**
 * Money is rendered with ToString("C") all over the views, which picks the
 * symbol off the host culture - so the same figure reads as $ on one machine
 * and Rs on another. The symbol is pinned here to keep the invoices consistent.
 */
var currencySymbol = builder.Configuration["NexusFinance:CurrencySymbol"];

if (!string.IsNullOrWhiteSpace(currencySymbol))
{
    var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
    culture.NumberFormat.CurrencySymbol = currencySymbol;
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}

/**
 * A non-nullable reference type would otherwise be treated as required by the
 * model validator. That contradicted the forms: fields the view labels
 * "optional" were rejected when left blank, so a vendor could not be saved
 * without a tax number and an order could not be placed without notes.
 * Requiredness is declared with [Required] on the view models, and that is now
 * the only thing that applies.
 */
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

var connectionString = builder.Configuration.GetConnectionString("NexusDb")
    ?? throw new InvalidOperationException("Connection string 'NexusDb' is not configured.");

builder.Services.AddDbContext<NexusDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        sql.CommandTimeout(30);
    }));

builder.Services.AddSingleton<IPasswordService, PasswordService>();

builder.Services.AddScoped<IUserAuthenticator, AuthenticationService>();
builder.Services.AddScoped<ISignInService, SignInService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IAccountIdGenerator, AccountIdGenerator>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IPlanCatalogService, PlanCatalogService>();
builder.Services.AddScoped<IFeasibilityService, FeasibilityService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICustomerPortalService, CustomerPortalService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAccountsDashboardService, AccountsDashboardService>();
builder.Services.AddScoped<ICallMeteringService, CallMeteringService>();
builder.Services.AddScoped<ITechnicalDashboardService, TechnicalDashboardService>();
builder.Services.AddScoped<IRetailDashboardService, RetailDashboardService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
builder.Services.AddScoped<IRetailRegisterService, RetailRegisterService>();
builder.Services.AddScoped<IPlanAdminService, PlanAdminService>();
builder.Services.AddScoped<IOrderTrackingService, OrderTrackingService>();
builder.Services.AddScoped<IPlanChangeService, PlanChangeService>();
builder.Services.AddScoped<IOrderRegisterService, OrderRegisterService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IContactMessageService, ContactMessageService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISettingsAdminService, SettingsAdminService>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IConnectionLifecycleService, ConnectionLifecycleService>();

builder.Services
    .AddAuthentication(SignInService.Scheme)
    .AddCookie(SignInService.Scheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Not sliding: an idle session should expire rather than renew itself.
        // This is the floor; the settings screen can raise it per sign-in.
        options.SlidingExpiration = false;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

        options.Cookie.Name = "NEXUS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // A rejected request to a protected page comes back as 403 rather than
        // a redirect when the caller expects JSON, which the AJAX helpers rely on.
        options.Events = new CookieAuthenticationEvents
        {
            // The session length is editable from the settings screen, so the
            // ticket is stamped at sign-in rather than fixed when the app starts.
            OnSigningIn = async ctx =>
            {
                // A "keep me signed in" ticket already carries its own window.
                if (ctx.Properties.ExpiresUtc is not null) return;

                var settings = ctx.HttpContext.RequestServices
                    .GetRequiredService<ISiteSettingsService>();

                var minutes = await settings.GetValueAsync(
                    "security.sessionMinutes", ctx.HttpContext.RequestAborted);

                if (int.TryParse(minutes, out var value) && value > 0)
                    ctx.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(value);
            },

            OnRedirectToLogin = ctx => HandleUnauthorizedAsync(ctx, StatusCodes.Status401Unauthorized),
            OnRedirectToAccessDenied = ctx => HandleUnauthorizedAsync(ctx, StatusCodes.Status403Forbidden)
        };
    });

builder.Services.AddHttpContextAccessor();

/**
 * A nonce per request lets the few inline script blocks we still have keep
 * working while everything else stays blocked by the policy. Generated before
 * the view renders so the tag helper and the header agree on the value.
 */
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

/**
 * Brute-force protection on the credentials screens. Ten attempts per IP per
 * minute is well above a human typo rate and low enough that a script hammering
 * the form is throttled within seconds. The queue is empty on purpose: once a
 * caller is over the limit they get a 429 rather than a spot in line.
 *
 * Both numbers come from configuration because an automated end-to-end run
 * (TestSprite and friends) drives many browser sessions through one address and
 * would otherwise trip the limiter on its own traffic. The defaults stay tight;
 * only a local override raises them.
 */
var loginPermitLimit = builder.Configuration.GetValue("Security:LoginRateLimit:PermitLimit", 10);
var loginWindowMinutes = builder.Configuration.GetValue("Security:LoginRateLimit:WindowMinutes", 1);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", http =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromMinutes(loginWindowMinutes),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            await context.HttpContext.Response.WriteAsync(
                "Too many attempts. Please wait a minute and try again.", token);
            return;
        }

        context.HttpContext.Response.Redirect("/Account/Login?throttled=1");
    };
});

var app = builder.Build();

/**
 * Exposes the per-request nonce to the views and to the header middleware.
 * Cheap enough to run on every request and keeps the two in sync.
 */
app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));

    context.Items["csp-nonce"] = nonce;
    await next();
});

/**
 * Reference data only - cities, plans, tiers, products and one admin account.
 * Every block checks before inserting, so restarting the app is harmless.
 */
static async Task SeedDatabaseAsync(WebApplication host)
{
    using var scope = host.Services.CreateScope();

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        await NexusSeeder.SeedAsync(db, hasher);
        Log.Information("Reference data seeded");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Seeding failed");
    }
}

/**
 * A fetch call should get a status code it can branch on, not a 200 with a
 * login page inside it. Full page navigations still get the redirect.
 */
static Task HandleUnauthorizedAsync(
    Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> ctx, int status)
{
    var wantsJson = ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    ctx.Request.Headers.Accept.ToString().Contains("application/json");

    if (wantsJson)
    {
        ctx.Response.StatusCode = status;
        return Task.CompletedTask;
    }

    ctx.Response.Redirect(ctx.RedirectUri);
    return Task.CompletedTask;
}

await SeedDatabaseAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error/500");
    app.UseHsts();
}
else
{
    // In development the stack trace stays visible, but the branded page is
    // still what a 404 or 403 shows.
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseHttpsRedirection();
app.UseNexusSecurityHeaders();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("NEXUS starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NEXUS terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

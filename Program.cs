using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NEXUS.Common.Abstractions;
using NEXUS.Data;
using NEXUS.Data.Seed;
using NEXUS.Services.Authentication;
using NEXUS.Services.Billing;
using NEXUS.Services.Catalog;
using NEXUS.Services.Feedback;
using NEXUS.Services.Orders;
using NEXUS.Services.Reporting;
using NEXUS.Services.CustomerPortal;
using NEXUS.Services.Dashboards;
using NEXUS.Services.Feasibility;
using NEXUS.Services.Generation;
using NEXUS.Services.Profile;
using NEXUS.Services.Registration;
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

builder.Services.AddControllersWithViews();

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
builder.Services.AddScoped<IAccountIdGenerator, AccountIdGenerator>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IPlanCatalogService, PlanCatalogService>();
builder.Services.AddScoped<IFeasibilityService, FeasibilityService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICustomerPortalService, CustomerPortalService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAccountsDashboardService, AccountsDashboardService>();
builder.Services.AddScoped<ITechnicalDashboardService, TechnicalDashboardService>();
builder.Services.AddScoped<IRetailDashboardService, RetailDashboardService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
builder.Services.AddScoped<IPlanAdminService, PlanAdminService>();
builder.Services.AddScoped<IOrderTrackingService, OrderTrackingService>();
builder.Services.AddScoped<IOrderRegisterService, OrderRegisterService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IContactMessageService, ContactMessageService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISettingsAdminService, SettingsAdminService>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<IConnectionLifecycleService, ConnectionLifecycleService>();

builder.Services
    .AddAuthentication(SignInService.Scheme)
    .AddCookie(SignInService.Scheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Not sliding: an idle session should expire rather than renew itself.
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
            OnRedirectToLogin = ctx => HandleUnauthorizedAsync(ctx, StatusCodes.Status401Unauthorized),
            OnRedirectToAccessDenied = ctx => HandleUnauthorizedAsync(ctx, StatusCodes.Status403Forbidden)
        };
    });

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

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
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

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

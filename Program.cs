using NEXUS.Services.Api;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// MVC (controllers + Razor views). NEXUS is a frontend-only
// project — no database, no authentication backend.
// ---------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------
// Register the API abstraction. The backend developer will swap
// ApiService (mock) for a real HTTP client implementation later.
// ---------------------------------------------------------------
builder.Services.AddSingleton<IApiService, ApiService>();

// HttpContext access for view-level user helpers (demo purposes).
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ---------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();   // serves wwwroot/css, wwwroot/js, wwwroot/images
app.UseRouting();
app.UseAuthorization();

// Default route: Home/Index is the public landing page.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
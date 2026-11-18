using System.Net;
using System.Threading.RateLimiting;
using AOB.DAL.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// MVC with a global [Authorize]; public pages opt out with [AllowAnonymous].
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<AOB.WEB.Filters.CustomerShellViewDataFilter>();
});
builder.Services.AddScoped<AOB.WEB.Filters.CustomerShellViewDataFilter>();

// DbContext, repositories, unit of work and services (see AOB.DAL.Common.DependencyInjection).
builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<AOB.WEB.Services.HealthCheckSettings>(
    builder.Configuration.GetSection(AOB.WEB.Services.HealthCheckSettings.SectionName));
builder.Services.Configure<AOB.WEB.Services.IpGeolocationSettings>(
    builder.Configuration.GetSection(AOB.WEB.Services.IpGeolocationSettings.SectionName));
builder.Services.AddHttpClient<AOB.WEB.Services.IIpGeolocator, AOB.WEB.Services.HttpIpGeolocator>(
    client => client.Timeout = TimeSpan.FromSeconds(4));
builder.Services.AddSingleton<AOB.WEB.Services.IpAccessTracker>();
builder.Services.AddHostedService<AOB.WEB.Services.ProjectDeletionWorker>();
builder.Services.AddScoped<AOB.BAL.Interfaces.Common.ICurrentActor, AOB.WEB.Services.CurrentActor>();

builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = ".AOB.Auth";
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Development also runs over plain http://localhost, so the Secure flag is enforced everywhere else.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

        // Re-check the account at most every 2 minutes so disabling a user or customer ends their session,
        // without hitting the database on every request (the last check time is kept inside the auth cookie).
        options.Events.OnValidatePrincipal = async context =>
        {
            const string checkedAtKey = ".aob.checkedAt";
            var now = DateTimeOffset.UtcNow;

            if (context.Properties.Items.TryGetValue(checkedAtKey, out var lastValue)
                && DateTimeOffset.TryParse(lastValue, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var lastChecked)
                && now - lastChecked < TimeSpan.FromMinutes(2))
            {
                return;
            }

            var idValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var users = context.HttpContext.RequestServices
                .GetRequiredService<AOB.BAL.Interfaces.Repositories.IUserAuthRepository>();
            var auth = Guid.TryParse(idValue, out var userId) ? await users.GetByIdAsync(userId) : null;

            var blocked = auth is null
                || !auth.IsActive
                || (auth.UserType == AOB.BAL.Common.Enums.UserType.Customer
                    && auth.Customer?.Status == AOB.BAL.Common.Enums.CustomerStatus.Disabled);

            if (blocked)
            {
                context.RejectPrincipal();
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    context.HttpContext,
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            context.Properties.Items[checkedAtKey] = now.ToString("O");
            context.ShouldRenew = true;
        };
    });

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

// Cookies and antiforgery tokens are protected with keys that must survive restarts and be shared by every instance.
// Set "DataProtection:KeyPath" to a persistent (ideally access-restricted) folder or share; unset keeps the framework default.
var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
    var dataProtection = builder.Services.AddDataProtection()
        .SetApplicationName("AOB")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));

    if (OperatingSystem.IsWindows())
    {
        dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
    }
}

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
});

// Reverse proxy: X-Forwarded-For is trusted only when "ForwardedHeaders:KnownProxies" / "KnownNetworks" are configured.
// Without it RemoteIpAddress is the proxy, which breaks the IP block list and the health-page allow-list.
var forwardedSection = builder.Configuration.GetSection("ForwardedHeaders");
var knownProxies = forwardedSection.GetSection("KnownProxies").Get<string[]>() ?? [];
var knownNetworks = forwardedSection.GetSection("KnownNetworks").Get<string[]>() ?? [];
var useForwardedHeaders = knownProxies.Length > 0 || knownNetworks.Length > 0;
if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        foreach (var proxy in knownProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }
        foreach (var network in knownNetworks)
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    });
}

// Throttles the anonymous sign-in, sign-up and recovery endpoints per client address (see [EnableRateLimiting] on AuthController).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static string Client(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.AddPolicy("auth-login", context => RateLimitPartition.GetFixedWindowLimiter(
        "login:" + Client(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    options.AddPolicy("auth-sensitive", context => RateLimitPartition.GetFixedWindowLimiter(
        "sensitive:" + Client(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    options.AddPolicy("auth-lookup", context => RateLimitPartition.GetFixedWindowLimiter(
        "lookup:" + Client(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

// Apply migrations and create the default PCIPAL partner login.
await DbSeeder.InitializeAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

app.UseHttpsRedirection();

app.UseHttpsRedirection();

// Browser hardening. Inline scripts/styles are still used by the views, so 'unsafe-inline' stays until they move to nonces.
// The PCI Pal flow is framed from, and posted to, https://*.cloud.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com data:; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-src https://*.cloud; " +
        "form-action 'self' https://*.cloud; " +
        "frame-ancestors 'none'; base-uri 'self'; object-src 'none'";
    headers["X-Frame-Options"] = "DENY";
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
    await next();
});

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();

// Blocked addresses get a 403 on everything; every other page request is counted against its address.
app.Use(async (context, next) =>
{
    if (context.Connection.RemoteIpAddress is { } remote)
    {
        var tracker = context.RequestServices.GetRequiredService<AOB.WEB.Services.IpAccessTracker>();
        var ip = AOB.WEB.Services.IpAccessTracker.Normalize(remote);
        if (await tracker.IsBlockedAsync(ip))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied.");
            return;
        }

        var path = context.Request.Path;
        if (!Path.HasExtension(path.Value) && !path.StartsWithSegments("/health"))
        {
            tracker.Track(ip, path.Value ?? "/", context.User.Identity?.Name);
        }
    }

    await next();
});

app.UseAuthorization();

// An upload over the endpoint's size limit goes back to its page with a message instead of a bare 400/413 page.
app.UseMiddleware<AOB.WEB.Middleware.OversizedUploadMiddleware>();

app.MapStaticAssets();

// A partner works on a customer's projects (customers have no login of their own). The customer id in the URL
// is carried into every project link on the page and is checked against the partner in ProjectsController.
app.MapControllerRoute(
    name: "partner-customer-projects",
    pattern: "Customers/{customerId:guid}/Projects/{action=Index}/{id?}",
    defaults: new { controller = "Projects" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

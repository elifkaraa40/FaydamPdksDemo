using FaydamPDKS.Data;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using FaydamPDKS.Web.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: false,
    reloadOnChange: true);

builder.Services.AddPdksData(builder.Configuration);
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[] { new CultureInfo("tr-TR"), new CultureInfo("en-US") };
    options.DefaultRequestCulture = new RequestCulture("tr-TR");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
    {
        var culture = context.Request.Cookies["Faydam.Language"] == "en" ? "en-US" : "tr-TR";
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture));
    }));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-FaydamPdks.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            if (!Guid.TryParse(context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
                || !Guid.TryParse(context.Principal?.FindFirst("sid")?.Value, out var sessionId)
                || !await context.HttpContext.RequestServices
                    .GetRequiredService<WebDeviceSessionService>()
                    .ValidateAndTouchAsync(userId, sessionId, context.HttpContext.RequestAborted))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<WebDeviceSessionService>();
builder.Services.AddSingleton<IWebPasswordResetEmailSender, SmtpPasswordResetEmailSender>();
builder.Services.AddScoped<IWebLeaveApprovalService, WebLeaveApprovalService>();
builder.Services.AddScoped<IEmployeeAdminService, WebEmployeeAdminService>();
builder.Services.AddScoped<IShiftAdminService, WebShiftAdminService>();
builder.Services.AddScoped<IWebAttendanceCorrectionService, WebAttendanceCorrectionService>();
builder.Services.AddScoped<IOrganizationAdminService, WebOrganizationAdminService>();
builder.Services.AddScoped<IWorkCalendarAdminService, WebWorkCalendarAdminService>();
builder.Services.AddHostedService<PublicHolidayCalendarWorker>();
builder.Services.AddScoped<WebReportingService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));
    options.AddPolicy("password-reset", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0
        }));
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; font-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseMiddleware<UiTranslationMiddleware>();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true
        && context.User.FindFirst("must_change_password")?.Value == "true")
    {
        var path = context.Request.Path;
        var allowed = path.StartsWithSegments("/Home/Account")
            || path.StartsWithSegments("/Home/ChangePassword")
            || path.StartsWithSegments("/Home/Logout");
        if (!allowed)
        {
            context.Response.Redirect("/Home/Account?section=security");
            return;
        }
    }
    await next();
});
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        return Results.Ok(new { status = "ready", database = "healthy" });
    }
    catch
    {
        return Results.Json(new { status = "not_ready", database = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedDemoData"))
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

app.Run();

public sealed class FaydamPdksWebMarker;

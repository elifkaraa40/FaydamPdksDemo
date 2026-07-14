using FaydamPDKS.Api;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: false,
    reloadOnChange: true);

builder.Services.AddPdksData(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IMobileAuthService, MobileTokenService>();
builder.Services.AddScoped<IAttendanceService, MobileAttendanceService>();
builder.Services.AddScoped<ILeaveRequestService, MobileLeaveRequestService>();
builder.Services.AddScoped<IMobileProfileService, MobileProfileService>();
builder.Services.AddScoped<IMobileNotificationService, MobileNotificationService>();
builder.Services.AddScoped<IAttendanceCorrectionService, MobileAttendanceCorrectionService>();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(x => string.IsNullOrWhiteSpace(x.Key) ? "request" : x.Key,
                x => x.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Geçersiz değer." : error.ErrorMessage).ToArray());
        return new BadRequestObjectResult(new ApiErrorDto("VALIDATION_ERROR", "İstek doğrulanamadı.", errors, context.HttpContext.TraceIdentifier));
    };
});
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FaydamPDKS Mobil API",
        Version = "v1",
        Description = "FaydamPDKS mobil uygulaması için sürümlenmiş API sözleşmesi."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key en az 32 karakter olmalı ve secret store üzerinden sağlanmalıdır.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "name",
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("mobile-auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));
    options.AddPolicy("terminal-heartbeat", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Cache-Control"] = "no-store";
    await next();
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
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
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    try { await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken); return Results.Ok(new { status = "ready" }); }
    catch { return Results.Json(new { status = "not_ready" }, statusCode: StatusCodes.Status503ServiceUnavailable); }
}).AllowAnonymous();
app.Run();

public sealed class FaydamPdksApiMarker;

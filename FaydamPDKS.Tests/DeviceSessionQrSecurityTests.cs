using FaydamPDKS.Api;
using FaydamPDKS.Api.Controllers;
using FaydamPDKS.Core.Attendance;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class DeviceSessionQrSecurityTests
{
    private const string DeviceA = "device-a-installation-id";
    private const string DeviceB = "device-b-installation-id";

    [Fact]
    public async Task Qr_from_revoked_device_is_rejected_without_creating_attendance()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 23, 8, 0, 0, TimeSpan.Zero);
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Personel",
            NormalizedName = "PERSONEL"
        };
        var workplace = new Workplace
        {
            Id = Guid.NewGuid(),
            Code = "MERKEZ",
            Name = "Merkez",
            TimeZoneId = "Europe/Istanbul",
            IsActive = true
        };
        var zone = new Zone
        {
            Name = "Ana Giriş",
            WorkplaceId = workplace.Id,
            Workplace = workplace,
            IsActive = true
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test Personel",
            Email = "qr-device@faydam.com",
            EmployeeNumber = "QR-1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!"),
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };
        const string rawQr = "secure-entry-qr";
        var qrCode = new AttendanceQrCode
        {
            Id = Guid.NewGuid(),
            WorkplaceId = workplace.Id,
            Workplace = workplace,
            Zone = zone,
            Name = "Giriş",
            EventType = AttendanceEventType.Entry,
            TokenHash = AttendanceQrService.Hash(rawQr),
            IsActive = true,
            CreatedAt = now
        };
        context.AddRange(role, workplace, zone, user, qrCode);
        await context.SaveChangesAsync();

        var auth = CreateAuthService(context, now);
        var first = await auth.LoginAsync(new MobileLoginRequest(
            user.Email, "StrongPassword123!", DeviceA, "Telefon A"));
        await auth.LoginAsync(new MobileLoginRequest(
            user.Email, "StrongPassword123!", DeviceB, "Telefon B"));

        var controller = new MobileAttendanceQrController(
            new AttendanceQrService(context, new TestTimeProvider(now)),
            auth)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("sub", user.Id.ToString()),
                        new Claim("sid", first!.DeviceSessionId.ToString())
                    ], "test"))
                }
            }
        };

        var result = await controller.Scan(
            new ScanAttendanceQrRequest(
                rawQr, now, "revoked-device-event", DeviceA),
            CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Empty(await context.AccessLogs.ToListAsync());
    }

    private static MobileTokenService CreateAuthService(
        AppDbContext context,
        DateTimeOffset now)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-only-key-with-more-than-32-characters-long",
            ["Jwt:Issuer"] = "FaydamPDKS.Tests",
            ["Jwt:Audience"] = "FaydamPDKS.Mobile.Tests",
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:RefreshTokenDays"] = "30"
        };
        return new MobileTokenService(
            new UserRepository(context),
            new RefreshTokenRepository(context),
            new UnitOfWork(context),
            context,
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
            new TestTimeProvider(now));
    }
}

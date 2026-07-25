using FaydamPDKS.Core.Models;
using FaydamPDKS.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class WebDeviceSessionServiceTests
{
    [Fact]
    public async Task Manager_web_login_keeps_mobile_session_and_creates_a_second_active_session()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 25, 10, 0, 0, TimeSpan.Zero);
        var user = CreateManager();
        context.Add(user);
        context.DeviceSessions.Add(new DeviceSession
        {
            UserId = user.Id,
            DeviceIdHash = new string('A', 64),
            DeviceName = "Android telefon",
            LoggedInAt = now.AddMinutes(-10),
            LastActiveAt = now.AddMinutes(-1)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, now);
        var webSession = await service.OpenAsync(user, "browser-installation-id", "Google Chrome · Windows (Web)");

        var activeSessions = await context.DeviceSessions
            .Where(x => x.UserId == user.Id && x.RevokedAt == null)
            .ToListAsync();
        Assert.Equal(2, activeSessions.Count);
        Assert.Contains(activeSessions, x => x.Id == webSession.Id && x.DeviceName.Contains("Web"));
        Assert.Contains(activeSessions, x => x.DeviceName == "Android telefon");
    }

    [Fact]
    public async Task Revoked_web_session_is_rejected_by_cookie_validation()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 25, 10, 0, 0, TimeSpan.Zero);
        var user = CreateManager();
        context.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);
        var session = await service.OpenAsync(user, "browser-installation-id", "Firefox · Windows (Web)");

        Assert.True(await service.ValidateAndTouchAsync(user.Id, session.Id));
        await service.RevokeAsync(user.Id, session.Id);

        Assert.False(await service.ValidateAndTouchAsync(user.Id, session.Id));
    }

    private static User CreateManager()
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Yönetici",
            NormalizedName = "YONETICI"
        };
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Test Yönetici",
            Email = "manager-session@faydam.com",
            EmployeeNumber = "SESSION-1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!"),
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };
    }

    private static WebDeviceSessionService CreateService(
        FaydamPDKS.Data.AppDbContext context,
        DateTimeOffset now)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Security:AllowManagerMultipleDevices"] = "true"
            }).Build();
        return new WebDeviceSessionService(context, configuration, new TestTimeProvider(now));
    }
}

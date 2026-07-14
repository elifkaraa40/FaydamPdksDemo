using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class AttendanceTerminalServiceTests
{
    [Fact]
    public async Task Registration_returns_secret_once_and_valid_heartbeat_updates_health()
    {
        await using var context = TestInfrastructure.CreateContext();
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "IST", Name = "Merkez", TimeZoneId = "Europe/Istanbul", IsActive = true };
        context.Workplaces.Add(workplace);
        await context.SaveChangesAsync();
        var now = new DateTimeOffset(2026, 7, 14, 9, 0, 0, TimeSpan.Zero);
        var service = new AttendanceTerminalService(context, new TestTimeProvider(now));

        var registered = await service.RegisterAsync(new RegisterTerminalDto { WorkplaceId = workplace.Id, Name = "Ana Giriş", SerialNumber = " sn-001 " });
        var stored = await context.AttendanceTerminals.SingleAsync();
        Assert.NotEqual(registered.ApiKey, stored.ApiKeyHash);
        Assert.False(await service.HeartbeatAsync(registered.Id, "wrong-key", new TerminalHeartbeatDto()));

        Assert.True(await service.HeartbeatAsync(registered.Id, registered.ApiKey, new TerminalHeartbeatDto { FirmwareVersion = "1.2.3", PendingEventCount = 7 }));
        var item = Assert.Single((await service.GetPageAsync()).Terminals);
        Assert.True(item.IsOnline);
        Assert.Equal(7, item.PendingEventCount);
        Assert.Equal(now, item.LastSeenAt);

        var rotated = await service.RotateKeyAsync(registered.Id);
        Assert.NotNull(rotated);
        Assert.False(await service.HeartbeatAsync(registered.Id, registered.ApiKey, new TerminalHeartbeatDto()));
        Assert.True(await service.HeartbeatAsync(registered.Id, rotated.ApiKey, new TerminalHeartbeatDto()));
    }

    [Fact]
    public async Task Duplicate_serial_number_is_rejected()
    {
        await using var context = TestInfrastructure.CreateContext();
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "IST", Name = "Merkez", TimeZoneId = "Europe/Istanbul", IsActive = true };
        context.Workplaces.Add(workplace);
        await context.SaveChangesAsync();
        var service = new AttendanceTerminalService(context, new TestTimeProvider(DateTimeOffset.UtcNow));
        var request = new RegisterTerminalDto { WorkplaceId = workplace.Id, Name = "A", SerialNumber = "SN-1" };
        await service.RegisterAsync(request);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(request));
    }
}

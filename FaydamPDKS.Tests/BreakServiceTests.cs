using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class BreakServiceTests
{
    [Fact]
    public async Task Employee_with_active_entry_can_start_and_end_break()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero);
        var user = ActiveUser();
        context.Users.Add(user);
        context.AccessLogs.Add(new AccessLog
        {
            UserId = user.Id, ZoneId = 1, LogType = "Giris", LogDate = now.AddHours(-1).UtcDateTime
        });
        await context.SaveChangesAsync();
        var service = new BreakService(context, new TestTimeProvider(now));

        var started = await service.StartAsync(user.Id, "break-start-1");
        var ended = await service.EndAsync(user.Id, started.BreakId!.Value, "break-end-1");

        Assert.True(started.IsOnBreak);
        Assert.False(ended.IsOnBreak);
        var record = await context.BreakRecords.SingleAsync();
        Assert.Equal(now, record.StartedAt);
        Assert.Equal(now, record.EndedAt);
    }

    [Fact]
    public async Task Break_requires_entry_and_only_one_can_be_active()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero);
        var user = ActiveUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = new BreakService(context, new TestTimeProvider(now));

        var missingEntry = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(user.Id, "start-1"));
        Assert.Equal("BREAK_REQUIRES_ACTIVE_ATTENDANCE", missingEntry.Message);

        context.AccessLogs.Add(new AccessLog { UserId = user.Id, ZoneId = 1, LogType = "Giris", LogDate = now.UtcDateTime });
        await context.SaveChangesAsync();
        await service.StartAsync(user.Id, "start-2");
        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(user.Id, "start-3"));
        Assert.Equal("BREAK_ALREADY_ACTIVE", duplicate.Message);
    }

    [Fact]
    public async Task Previous_day_active_break_is_auto_closed_and_does_not_block_today()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero);
        var user = ActiveUser();
        context.Users.Add(user);
        context.BreakRecords.Add(new BreakRecord
        {
            UserId = user.Id,
            StartedAt = new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero),
            StartDeviceEventId = "yesterday-break"
        });
        context.AccessLogs.Add(new AccessLog
        {
            UserId = user.Id,
            ZoneId = 1,
            LogType = "Giris",
            LogDate = now.AddHours(-1).UtcDateTime
        });
        await context.SaveChangesAsync();
        var service = new BreakService(context, new TestTimeProvider(now));

        var current = await service.GetCurrentAsync(user.Id);
        var started = await service.StartAsync(user.Id, "today-break");

        Assert.False(current.IsOnBreak);
        Assert.True(started.IsOnBreak);
        var records = await context.BreakRecords.OrderBy(x => x.StartedAt).ToArrayAsync();
        Assert.True(records[0].AutoClosed);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 16, 21, 0, 0, TimeSpan.Zero),
            records[0].EndedAt);
        Assert.Null(records[1].EndedAt);
    }

    [Fact]
    public async Task Previous_day_entry_does_not_allow_starting_a_break_today()
    {
        await using var context = TestInfrastructure.CreateContext();
        var now = new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero);
        var user = ActiveUser();
        context.Users.Add(user);
        context.AccessLogs.Add(new AccessLog
        {
            UserId = user.Id,
            ZoneId = 1,
            LogType = "Giris",
            LogDate = new DateTime(2026, 7, 16, 18, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();
        var service = new BreakService(context, new TestTimeProvider(now));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(user.Id, "today-break"));

        Assert.Equal("BREAK_REQUIRES_ACTIVE_ATTENDANCE", error.Message);
    }

    private static User ActiveUser() => new()
    {
        Id = Guid.NewGuid(), Name = "Mola Test", Email = $"{Guid.NewGuid():N}@faydam.test",
        EmployeeNumber = Guid.NewGuid().ToString("N"), RoleId = Guid.NewGuid(), IsActive = true
    };
}

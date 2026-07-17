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

    private static User ActiveUser() => new()
    {
        Id = Guid.NewGuid(), Name = "Mola Test", Email = $"{Guid.NewGuid():N}@faydam.test",
        EmployeeNumber = Guid.NewGuid().ToString("N"), RoleId = Guid.NewGuid(), IsActive = true
    };
}

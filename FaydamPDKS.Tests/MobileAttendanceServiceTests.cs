using FaydamPDKS.Api;
using FaydamPDKS.Core.Attendance;
using FaydamPDKS.Core.DTOs.Attendance;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class MobileAttendanceServiceTests
{
    [Fact]
    public async Task Stores_device_event_once_and_returns_today_summary()
    {
        await using var context = TestInfrastructure.CreateContext();
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, Name = "Test", Email = "test@faydam.com", RoleId = Guid.NewGuid() });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var request = new CreateAttendanceEventRequest(
            AttendanceEventType.Entry,
            new DateTimeOffset(2026, 7, 14, 9, 12, 0, TimeSpan.FromHours(3)),
            "device-event-001",
            1);

        Assert.True(await service.AddEventAsync(userId, request));
        Assert.False(await service.AddEventAsync(userId, request));
        var summary = await service.GetTodayAsync(userId);
        Assert.Equal("MissingExit", summary.Status);
        Assert.NotNull(summary.FirstEntry);

        var history = await service.GetRangeAsync(userId, new DateOnly(2026, 7, 13), new DateOnly(2026, 7, 14));
        Assert.Equal(2, history.Count);
        Assert.Equal("NoRecord", history[0].Status);
        Assert.Equal("MissingExit", history[1].Status);
    }

    [Fact]
    public async Task Rejects_history_ranges_longer_than_ninety_days()
    {
        await using var context = TestInfrastructure.CreateContext();
        var service = CreateService(context);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRangeAsync(
            Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public async Task Uses_employee_shift_assignment_for_attendance_calculation()
    {
        await using var context = TestInfrastructure.CreateContext();
        var userId = Guid.NewGuid();
        var shift = new Shift
        {
            Id = Guid.NewGuid(), Name = "Erken Vardiya", StartsAt = new TimeOnly(8, 0),
            EndsAt = new TimeOnly(16, 0), BreakMinutes = 30, LateToleranceMinutes = 10, IsActive = true
        };
        context.Users.Add(new User { Id = userId, Name = "Shift User", Email = "shift@faydam.com", RoleId = Guid.NewGuid() });
        context.Shifts.Add(shift);
        context.EmployeeShiftAssignments.Add(new EmployeeShiftAssignment
        {
            Id = Guid.NewGuid(), EmployeeId = userId, ShiftId = shift.Id,
            ValidFrom = new DateOnly(2026, 7, 1)
        });
        context.AccessLogs.AddRange(
            new AccessLog { UserId = userId, ZoneId = 1, LogDate = new DateTime(2026, 7, 14, 5, 5, 0, DateTimeKind.Utc), LogType = "Giris" },
            new AccessLog { UserId = userId, ZoneId = 1, LogDate = new DateTime(2026, 7, 14, 13, 0, 0, DateTimeKind.Utc), LogType = "Cikis" });
        await context.SaveChangesAsync();

        var summary = await CreateService(context).GetTodayAsync(userId);

        Assert.Equal(450, summary.ExpectedMinutes);
        Assert.Equal(0, summary.LateMinutes);
        Assert.Equal("Complete", summary.Status);
    }

    private static MobileAttendanceService CreateService(AppDbContext context)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Attendance:TimeZone"] = "Europe/Istanbul",
            ["Attendance:DefaultShiftStart"] = "09:00",
            ["Attendance:DefaultShiftEnd"] = "18:00",
            ["Attendance:LateToleranceMinutes"] = "5",
            ["Attendance:EarlyLeaveToleranceMinutes"] = "5",
            ["Attendance:BreakMinutes"] = "60"
        }).Build();
        return new MobileAttendanceService(
            new AccessLogRepository(context), new ShiftResolver(context), new AttendanceCorrectionRepository(context), new WorkCalendarResolver(context), new BreakService(context, new TestTimeProvider(new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero))), new UnitOfWork(context), config,
            new TestTimeProvider(new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero)));
    }
}

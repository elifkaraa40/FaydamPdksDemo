using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class AttendanceReportServiceTests
{
    [Fact]
    public async Task Calculates_rows_from_assigned_shift_and_raw_logs()
    {
        await using var context = TestInfrastructure.CreateContext();
        var user = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-0200", Name = "Rapor Test", Email = "report@faydam.com", IsActive = true };
        var shift = new Shift { Id = Guid.NewGuid(), Name = "Erken", StartsAt = new TimeOnly(8, 0), EndsAt = new TimeOnly(16, 0), BreakMinutes = 30, IsActive = true };
        context.AddRange(user, shift);
        context.EmployeeShiftAssignments.Add(new EmployeeShiftAssignment { Id = Guid.NewGuid(), EmployeeId = user.Id, ShiftId = shift.Id, ValidFrom = new DateOnly(2026, 7, 1) });
        context.AccessLogs.AddRange(
            new AccessLog { UserId = user.Id, ZoneId = 1, LogDate = new DateTime(2026, 7, 14, 5, 0, 0, DateTimeKind.Utc), LogType = "Giris" },
            new AccessLog { UserId = user.Id, ZoneId = 1, LogDate = new DateTime(2026, 7, 14, 13, 0, 0, DateTimeKind.Utc), LogType = "Cikis" });
        await context.SaveChangesAsync();

        var report = await CreateService(context).GetAsync(new DateOnly(2026, 7, 14), new DateOnly(2026, 7, 14));

        var row = Assert.Single(report.Rows);
        Assert.Equal("Erken", row.ShiftName);
        Assert.Equal(450, row.ExpectedMinutes);
        Assert.Equal("Complete", row.Status);
    }

    [Fact]
    public async Task Rejects_ranges_longer_than_thirty_one_days()
    {
        await using var context = TestInfrastructure.CreateContext();
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(context).GetAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)));
    }

    private static AttendanceReportService CreateService(AppDbContext context) => new(context,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Attendance:TimeZone"] = "Europe/Istanbul",
            ["Attendance:DefaultShiftStart"] = "09:00",
            ["Attendance:DefaultShiftEnd"] = "18:00"
        }).Build());
}

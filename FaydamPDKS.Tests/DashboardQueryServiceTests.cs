using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class DashboardQueryServiceTests
{
    [Fact]
    public async Task Builds_last_seven_completed_days_from_distinct_daily_entries()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var first = new User { Id = Guid.NewGuid(), Name = "Bir", Email = "bir@test.com", Role = role, RoleId = role.Id };
        var second = new User { Id = Guid.NewGuid(), Name = "İki", Email = "iki@test.com", Role = role, RoleId = role.Id };
        context.AddRange(role, first, second);
        context.AccessLogs.AddRange(
            new AccessLog { UserId = first.Id, ZoneId = 1, LogDate = new DateTime(2026, 7, 24, 5, 30, 0, DateTimeKind.Utc), LogType = "Giris" },
            new AccessLog { UserId = first.Id, ZoneId = 1, LogDate = new DateTime(2026, 7, 24, 6, 0, 0, DateTimeKind.Utc), LogType = "Giris" },
            new AccessLog { UserId = first.Id, ZoneId = 1, LogDate = new DateTime(2026, 7, 27, 6, 0, 0, DateTimeKind.Utc), LogType = "Giris" });
        context.LeaveRequests.Add(new LeaveRequest
        {
            UserId = second.Id,
            User = second,
            StartDate = new DateOnly(2026, 7, 27),
            EndDate = new DateOnly(2026, 7, 27),
            Status = LeaveRequestStatus.Approved,
            CreatedAt = new DateTimeOffset(2026, 7, 20, 8, 0, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync();

        var service = new DashboardQueryService(context, Configuration(),
            new TestTimeProvider(new DateTimeOffset(2026, 7, 27, 8, 0, 0, TimeSpan.Zero)));

        var dashboard = await service.GetAsync();

        Assert.Equal(7, dashboard.DailyAttendance.Count);
        Assert.Equal(new DateOnly(2026, 7, 16), dashboard.DailyAttendance[0].WorkDate);
        Assert.Equal(new DateOnly(2026, 7, 24), dashboard.DailyAttendance[^1].WorkDate);
        var july24 = Assert.Single(dashboard.DailyAttendance, x => x.WorkDate == new DateOnly(2026, 7, 24));
        Assert.Equal(2, july24.TotalPersonnel);
        Assert.Equal(first.Id, Assert.Single(july24.OnTimePersonnel).Id);
        Assert.Empty(july24.LatePersonnel);
        Assert.Empty(july24.OnLeavePersonnel);
        Assert.Equal(second.Id, Assert.Single(july24.MissingRecordPersonnel).Id);
        Assert.Equal(first.Id, Assert.Single(dashboard.PresentPersonnel).Id);
        Assert.Equal(second.Id, Assert.Single(dashboard.OnLeavePersonnel).Id);
        Assert.Empty(dashboard.LatePersonnel);
        Assert.Empty(dashboard.MissingRecordPersonnel);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Attendance:TimeZone"] = "Europe/Istanbul",
            ["Attendance:DefaultShiftStart"] = "09:00",
            ["Attendance:LateToleranceMinutes"] = "5"
        })
        .Build();
}

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
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-0200", Name = "Rapor Test", Email = "report@faydam.com", Role = role, RoleId = role.Id, IsActive = true };
        var shift = new Shift { Id = Guid.NewGuid(), Name = "Erken", StartsAt = new TimeOnly(8, 0), EndsAt = new TimeOnly(16, 0), BreakMinutes = 30, IsActive = true };
        context.AddRange(role, user, shift);
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
    public async Task Rejects_ranges_longer_than_ninety_days()
    {
        await using var context = TestInfrastructure.CreateContext();
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(context).GetAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public async Task Excludes_managers_and_empty_weekends_and_supports_employee_filter()
    {
        await using var context = TestInfrastructure.CreateContext();
        var managerRole = new Role { Id = Guid.NewGuid(), Name = "Yonetici", NormalizedName = "YONETICI" };
        var personnelRole = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var manager = new User { Id = Guid.NewGuid(), EmployeeNumber = "YON-1", Name = "Yönetici", Email = "manager@test.com", Role = managerRole, RoleId = managerRole.Id, IsActive = true };
        var first = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-1", Name = "Bir", Email = "one@test.com", Role = personnelRole, RoleId = personnelRole.Id, IsActive = true };
        var second = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-2", Name = "İki", Email = "two@test.com", Role = personnelRole, RoleId = personnelRole.Id, IsActive = true };
        context.AddRange(managerRole, personnelRole, manager, first, second);
        await context.SaveChangesAsync();

        var report = await CreateService(context).GetAsync(new DateOnly(2026, 7, 17), new DateOnly(2026, 7, 19), first.Id);

        Assert.All(report.Rows, row => Assert.Equal(first.Id, row.EmployeeId));
        Assert.Single(report.Rows); // Friday; empty Saturday and Sunday are hidden.
        Assert.DoesNotContain(report.Employees!, x => x.Id == manager.Id);
        Assert.Equal(2, report.Employees!.Count);
    }

    [Fact]
    public async Task Reports_remote_work_details_and_orders_all_personnel_by_day()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var zeynep = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-2", Name = "Zeynep", Email = "z@test.com", Role = role, RoleId = role.Id, IsActive = true };
        var ahmet = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-1", Name = "Ahmet", Email = "a@test.com", Role = role, RoleId = role.Id, IsActive = true };
        context.AddRange(role, zeynep, ahmet);
        context.WorkLocationAssignments.Add(new WorkLocationAssignment
        {
            UserId = ahmet.Id, LocationType = FaydamPDKS.Core.Enums.WorkLocationType.Remote,
            StartDate = new DateOnly(2026, 7, 20), EndDate = new DateOnly(2026, 7, 20),
            RecurrenceType = FaydamPDKS.Core.Enums.WorkLocationRecurrenceType.Once,
            Reason = "Evden çalışma", CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow, IsActive = true
        });
        await context.SaveChangesAsync();

        var report = await CreateService(context).GetAsync(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 21));

        Assert.Collection(report.Rows,
            row => { Assert.Equal(new DateOnly(2026, 7, 20), row.WorkDate); Assert.Equal("Ahmet", row.EmployeeName); Assert.Equal("Remote", row.WorkLocation); Assert.Equal("Evden çalışma", row.WorkLocationDetail); },
            row => { Assert.Equal(new DateOnly(2026, 7, 20), row.WorkDate); Assert.Equal("Zeynep", row.EmployeeName); },
            row => { Assert.Equal(new DateOnly(2026, 7, 21), row.WorkDate); Assert.Equal("Ahmet", row.EmployeeName); },
            row => { Assert.Equal(new DateOnly(2026, 7, 21), row.WorkDate); Assert.Equal("Zeynep", row.EmployeeName); });
    }

    private static AttendanceReportService CreateService(AppDbContext context) => new(context,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Attendance:TimeZone"] = "Europe/Istanbul",
            ["Attendance:DefaultShiftStart"] = "09:00",
            ["Attendance:DefaultShiftEnd"] = "18:00"
        }).Build());
}

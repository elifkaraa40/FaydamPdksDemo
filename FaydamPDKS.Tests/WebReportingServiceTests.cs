using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using FaydamPDKS.Web;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class WebReportingServiceTests
{
    [Fact]
    public async Task Transition_report_converts_utc_to_turkey_time_and_filters_event_type()
    {
        await using var context = TestInfrastructure.CreateContext();
        var (employee, zone) = await SeedOrganizationAsync(context);
        context.AccessLogs.AddRange(
            new AccessLog { UserId = employee.Id, ZoneId = zone.Id, LogDate = new DateTime(2026, 7, 20, 5, 30, 0, DateTimeKind.Utc), LogType = "Giris", Source = "MobileQr" },
            new AccessLog { UserId = employee.Id, ZoneId = zone.Id, LogDate = new DateTime(2026, 7, 20, 15, 0, 0, DateTimeKind.Utc), LogType = "Cikis", Source = "MobileQr" });
        await context.SaveChangesAsync();
        var service = new WebReportingService(context, new WorkCalendarResolver(context));

        var report = await service.GetTransitionsAsync(new DateOnly(2026, 7, 20),
            new DateOnly(2026, 7, 20), employee.Id, "Giris");

        var row = Assert.Single(report.Rows);
        Assert.Equal(new TimeOnly(8, 30), row.EventTime);
        Assert.Equal("Giris", row.EventType);
        Assert.Equal("İstanbul", row.WorkplaceName);
    }

    [Fact]
    public async Task Leave_report_lists_overlapping_requests_and_calculates_work_days()
    {
        await using var context = TestInfrastructure.CreateContext();
        var (employee, _) = await SeedOrganizationAsync(context);
        context.LeaveRequests.AddRange(
            new LeaveRequest
            {
                UserId = employee.Id, User = employee, LeaveType = LeaveType.Annual,
                StartDate = new DateOnly(2026, 7, 20), EndDate = new DateOnly(2026, 7, 21),
                DayPortion = LeaveDayPortion.FullDay, Status = LeaveRequestStatus.Approved
            },
            new LeaveRequest
            {
                UserId = employee.Id, User = employee, LeaveType = LeaveType.Unpaid,
                StartDate = new DateOnly(2026, 8, 10), EndDate = new DateOnly(2026, 8, 10),
                DayPortion = LeaveDayPortion.FullDay, Status = LeaveRequestStatus.Pending
            });
        await context.SaveChangesAsync();
        var service = new WebReportingService(context, new WorkCalendarResolver(context));

        var report = await service.GetLeavesAsync(new DateOnly(2026, 7, 21),
            new DateOnly(2026, 7, 25), employee.Id, LeaveType.Annual, LeaveRequestStatus.Approved);

        var row = Assert.Single(report.Rows);
        Assert.Equal(1, row.WorkDayCount);
        Assert.Equal(LeaveType.Annual, row.LeaveType);
    }

    private static async Task<(User Employee, Zone Zone)> SeedOrganizationAsync(AppDbContext context)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "IST", Name = "İstanbul", TimeZoneId = "Europe/Istanbul" };
        var department = new Department { Id = Guid.NewGuid(), WorkplaceId = workplace.Id, Workplace = workplace, Code = "OPS", Name = "Operasyon" };
        var employee = new User
        {
            Id = Guid.NewGuid(), EmployeeNumber = "PER-1", Name = "Elif", Email = "elif@example.com",
            RoleId = role.Id, Role = role, WorkplaceId = workplace.Id, Workplace = workplace,
            DepartmentId = department.Id, Department = department, IsActive = true
        };
        var zone = new Zone { Id = 1, Name = "Ana giriş", WorkplaceId = workplace.Id, Workplace = workplace };
        context.AddRange(role, workplace, department, employee, zone);
        await context.SaveChangesAsync();
        return (employee, zone);
    }
}

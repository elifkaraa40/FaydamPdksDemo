using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using FaydamPDKS.Web;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class WorkCalendarTests
{
    [Fact]
    public async Task Workplace_rule_overrides_global_holiday_and_weekend_default()
    {
        await using var context = TestInfrastructure.CreateContext();
        var workplace = new Workplace { Id = Guid.NewGuid(), Code = "IST", Name = "İstanbul", TimeZoneId = "Europe/Istanbul", IsActive = true };
        var employee = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-1", Name = "Test", Email = "t@f.com", WorkplaceId = workplace.Id, Workplace = workplace, IsActive = true };
        var saturday = new DateOnly(2026, 7, 18);
        context.AddRange(workplace, employee,
            new WorkCalendarDay { Id = Guid.NewGuid(), Date = saturday, Name = "Genel tatil", DayType = CalendarDayType.Holiday },
            new WorkCalendarDay { Id = Guid.NewGuid(), WorkplaceId = workplace.Id, Date = saturday, Name = "Telafi çalışması", DayType = CalendarDayType.WorkingDayOverride });
        await context.SaveChangesAsync();

        var result = await new WorkCalendarResolver(context).ResolveAsync(employee.Id, saturday);
        Assert.True(result.IsWorkingDay);
        Assert.Equal("Telafi çalışması", result.Name);
    }

    [Fact]
    public async Task Admin_rejects_duplicate_day_in_same_scope()
    {
        await using var context = TestInfrastructure.CreateContext();
        var service = new WebWorkCalendarAdminService(new WorkCalendarRepository(context), new OrganizationRepository(context), new UnitOfWork(context));
        var request = new CreateWorkCalendarDayDto { Date = new DateOnly(2026, 10, 29), Name = "Cumhuriyet Bayramı", DayType = CalendarDayType.Holiday };
        await service.CreateAsync(request);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
    }
}

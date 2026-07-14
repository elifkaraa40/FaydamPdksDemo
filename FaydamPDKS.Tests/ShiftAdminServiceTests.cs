using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using FaydamPDKS.Web;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class ShiftAdminServiceTests
{
    [Fact]
    public async Task Creates_overnight_shift()
    {
        await using var context = TestInfrastructure.CreateContext();
        var service = CreateService(context);

        await service.CreateShiftAsync(new CreateShiftDto
        {
            Name = "Gece 22-06", StartsAt = new TimeOnly(22, 0), EndsAt = new TimeOnly(6, 0),
            BreakMinutes = 30, LateToleranceMinutes = 5, EarlyLeaveToleranceMinutes = 5
        });

        var shift = Assert.Single(context.Shifts);
        Assert.Equal(new TimeOnly(22, 0), shift.StartsAt);
        Assert.True(shift.IsActive);
    }

    [Fact]
    public async Task Rejects_break_equal_to_shift_duration()
    {
        await using var context = TestInfrastructure.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(context).CreateShiftAsync(new CreateShiftDto
        {
            Name = "Invalid", StartsAt = new TimeOnly(9, 0), EndsAt = new TimeOnly(10, 0), BreakMinutes = 60
        }));
    }

    [Fact]
    public async Task Rejects_overlapping_employee_assignment()
    {
        await using var context = TestInfrastructure.CreateContext();
        var employee = new User { Id = Guid.NewGuid(), EmployeeNumber = "PER-0100", Name = "Test", Email = "test@faydam.com", IsActive = true };
        var shift = new Shift { Id = Guid.NewGuid(), Name = "Standart", StartsAt = new TimeOnly(9, 0), EndsAt = new TimeOnly(18, 0), IsActive = true };
        context.AddRange(employee, shift);
        context.EmployeeShiftAssignments.Add(new EmployeeShiftAssignment
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id, ShiftId = shift.Id,
            ValidFrom = new DateOnly(2026, 7, 1), ValidTo = new DateOnly(2026, 7, 31)
        });
        await context.SaveChangesAsync();

        var request = new CreateShiftAssignmentDto
        {
            EmployeeId = employee.Id, ShiftId = shift.Id,
            ValidFrom = new DateOnly(2026, 7, 20), ValidTo = new DateOnly(2026, 8, 10)
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(context).AssignAsync(request));
    }

    private static WebShiftAdminService CreateService(AppDbContext context) => new(
        new ShiftAdminRepository(context), new UserRepository(context), new UnitOfWork(context));
}

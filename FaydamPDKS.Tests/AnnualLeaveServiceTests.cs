using FaydamPDKS.Core;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Exceptions;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class AnnualLeaveServiceTests
{
    [Fact]
    public async Task Employee_cannot_request_annual_leave_before_first_anniversary()
    {
        await using var context = TestInfrastructure.CreateContext();
        var user = await SeedUserAsync(
            context,
            new DateOnly(2025, 7, 15),
            new DateOnly(1995, 1, 1));
        var service = CreateService(context, new DateOnly(2026, 7, 14));

        var balance = await service.GetBalanceAsync(user.Id);
        var error = await Assert.ThrowsAsync<AnnualLeaveException>(() =>
            service.EnsureCanRequestAsync(
                user.Id,
                new DateOnly(2026, 7, 20),
                new DateOnly(2026, 7, 24),
                LeaveDayPortion.FullDay));

        Assert.False(balance.IsEligible);
        Assert.Equal(0, balance.TotalEntitledDays);
        Assert.Equal(new DateOnly(2026, 7, 15), balance.FirstEntitlementDate);
        Assert.Equal("ANNUAL_LEAVE_NOT_EARNED", error.Code);
        Assert.Contains("15.07.2026", error.Message);
    }

    [Fact]
    public async Task Approved_and_pending_requests_reduce_available_balance_and_holidays_do_not_count()
    {
        await using var context = TestInfrastructure.CreateContext();
        var user = await SeedUserAsync(
            context,
            new DateOnly(2025, 1, 1),
            new DateOnly(1995, 1, 1));
        context.WorkCalendarDays.Add(new WorkCalendarDay
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 7, 21),
            Name = "Resmî tatil",
            DayType = CalendarDayType.Holiday,
            IsSystemGenerated = true
        });
        context.LeaveRequests.AddRange(
            NewAnnualLeave(user.Id, new DateOnly(2026, 7, 15), new DateOnly(2026, 7, 17), LeaveRequestStatus.Approved),
            NewAnnualLeave(user.Id, new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 22), LeaveRequestStatus.Pending));
        await context.SaveChangesAsync();
        var service = CreateService(context, new DateOnly(2026, 7, 14));

        var balance = await service.GetBalanceAsync(user.Id);

        Assert.Equal(14, balance.TotalEntitledDays);
        Assert.Equal(3, balance.ApprovedUsedDays);
        Assert.Equal(2, balance.PendingDays);
        Assert.Equal(9, balance.AvailableDays);
        Assert.Contains("bekleyen", balance.InformationMessage);
    }

    [Fact]
    public async Task Request_exceeding_available_balance_is_rejected_with_clear_amounts()
    {
        await using var context = TestInfrastructure.CreateContext();
        var user = await SeedUserAsync(
            context,
            new DateOnly(2025, 1, 1),
            new DateOnly(1995, 1, 1));
        context.LeaveRequests.Add(
            NewAnnualLeave(
                user.Id,
                new DateOnly(2026, 7, 15),
                new DateOnly(2026, 7, 24),
                LeaveRequestStatus.Pending));
        await context.SaveChangesAsync();
        var service = CreateService(context, new DateOnly(2026, 7, 14));

        var error = await Assert.ThrowsAsync<AnnualLeaveException>(() =>
            service.EnsureCanRequestAsync(
                user.Id,
                new DateOnly(2026, 7, 27),
                new DateOnly(2026, 8, 7),
                LeaveDayPortion.FullDay));

        Assert.Equal("ANNUAL_LEAVE_BALANCE_INSUFFICIENT", error.Code);
        Assert.Contains("10 gün", error.Message);
        Assert.Contains("6 gün", error.Message);
    }

    [Theory]
    [InlineData(1, 30, 14)]
    [InlineData(5, 30, 14)]
    [InlineData(6, 30, 20)]
    [InlineData(14, 30, 20)]
    [InlineData(15, 30, 26)]
    [InlineData(1, 18, 20)]
    [InlineData(1, 50, 20)]
    public void Legal_service_year_and_age_rules_are_applied(
        int serviceYear,
        int age,
        int expected)
    {
        Assert.Equal(
            expected,
            AnnualLeavePolicy.EntitlementForServiceYear(serviceYear, age));
    }

    private static AnnualLeaveService CreateService(AppDbContext context, DateOnly today) =>
        new(
            context,
            new WorkCalendarResolver(context),
            new TestTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

    private static async Task<User> SeedUserAsync(
        AppDbContext context,
        DateOnly hireDate,
        DateOnly birthDate)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Personel",
            NormalizedName = "PERSONEL"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Yıllık İzin Test",
            Email = $"{Guid.NewGuid():N}@test.local",
            EmployeeNumber = $"PER-{Guid.NewGuid():N}",
            RoleId = role.Id,
            Role = role,
            HireDate = hireDate,
            BirthDate = birthDate,
            IsActive = true
        };
        context.AddRange(role, user);
        await context.SaveChangesAsync();
        return user;
    }

    private static LeaveRequest NewAnnualLeave(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        LeaveRequestStatus status) =>
        new()
        {
            UserId = userId,
            LeaveType = LeaveType.Annual,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };
}

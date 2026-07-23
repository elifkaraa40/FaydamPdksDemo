using FaydamPDKS.Api;
using FaydamPDKS.Api.Controllers;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Exceptions;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class MobileLeaveRequestServiceTests
{
    [Fact]
    public async Task Creates_leave_and_rejects_overlapping_request()
    {
        await using var context = TestInfrastructure.CreateContext();
        var userId = await SeedUserAsync(context);
        var service = CreateService(context);
        var request = new CreateLeaveRequestDto(LeaveType.Annual, new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 24), "Tatil");

        var created = await service.CreateAsync(userId, request);

        Assert.Equal(LeaveRequestStatus.Pending, created.Status);
        Assert.Equal(5, created.CalendarDayCount);
        var overlap = await Assert.ThrowsAsync<LeaveOverlapException>(() => service.CreateAsync(
            userId,
            new CreateLeaveRequestDto(LeaveType.Excuse, new DateOnly(2026, 7, 24), new DateOnly(2026, 7, 25), null)));
        Assert.Equal(new DateOnly(2026, 7, 20), overlap.ConflictingStartDate);
        Assert.Equal(new DateOnly(2026, 7, 24), overlap.ConflictingEndDate);
    }

    [Fact]
    public async Task Controller_returns_standard_conflict_code_and_dates()
    {
        await using var context = TestInfrastructure.CreateContext();
        var userId = await SeedUserAsync(context);
        var service = CreateService(context);
        await service.CreateAsync(userId, new CreateLeaveRequestDto(
            LeaveType.Annual,
            new DateOnly(2026, 7, 20),
            new DateOnly(2026, 7, 24),
            "Tatil"));
        var controller = new MobileLeaveRequestsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("sub", userId.ToString())
                    ], "test"))
                }
            }
        };

        var result = await controller.Create(new CreateLeaveRequestDto(
            LeaveType.Excuse,
            new DateOnly(2026, 7, 23),
            new DateOnly(2026, 7, 25),
            "Çakışan talep"), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var error = Assert.IsType<ApiErrorDto>(conflict.Value);
        Assert.Equal("LEAVE_OVERLAP", error.Code);
        Assert.Equal("2026-07-20", error.Errors!["conflictingStartDate"].Single());
        Assert.Equal("2026-07-24", error.Errors["conflictingEndDate"].Single());
    }

    [Theory]
    [InlineData(LeaveType.Sick)]
    [InlineData(LeaveType.Excuse)]
    [InlineData(LeaveType.Unpaid)]
    public async Task Active_leave_blocks_overlapping_request_of_every_other_type(
        LeaveType secondLeaveType)
    {
        await using var context = TestInfrastructure.CreateContext();
        var userId = await SeedUserAsync(context);
        var service = CreateService(context);
        await service.CreateAsync(userId, new CreateLeaveRequestDto(
            LeaveType.Annual,
            new DateOnly(2026, 7, 23),
            new DateOnly(2026, 7, 23),
            "Yıllık izin"));

        var overlap = await Assert.ThrowsAsync<LeaveOverlapException>(() =>
            service.CreateAsync(userId, new CreateLeaveRequestDto(
                secondLeaveType,
                new DateOnly(2026, 7, 23),
                new DateOnly(2026, 7, 23),
                "Farklı izin türü")));

        Assert.Equal(new DateOnly(2026, 7, 23), overlap.ConflictingStartDate);
        Assert.Equal(new DateOnly(2026, 7, 23), overlap.ConflictingEndDate);
        Assert.Single(context.LeaveRequests);
    }

    [Fact]
    public async Task Cancels_only_pending_own_request()
    {
        await using var context = TestInfrastructure.CreateContext();
        var userId = await SeedUserAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(userId,
            new CreateLeaveRequestDto(LeaveType.Annual, new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 21), null));

        Assert.True(await service.CancelAsync(userId, created.Id));
        Assert.False(await service.CancelAsync(Guid.NewGuid(), created.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelAsync(userId, created.Id));
    }

    [Fact]
    public async Task Counts_friday_to_monday_as_two_workdays_and_supports_half_day()
    {
        await using var context = TestInfrastructure.CreateContext();
        var userId = await SeedUserAsync(context);
        var service = CreateService(context);

        var range = await service.CreateAsync(userId,
            new CreateLeaveRequestDto(LeaveType.Annual, new DateOnly(2026, 7, 17), new DateOnly(2026, 7, 20), null));
        Assert.Equal(4, range.CalendarDayCount);
        Assert.Equal(2, range.WorkDayCount);
        await service.CancelAsync(userId, range.Id);

        var halfDay = await service.CreateAsync(userId,
            new CreateLeaveRequestDto(LeaveType.Excuse, new DateOnly(2026, 7, 21), new DateOnly(2026, 7, 21), null, LeaveDayPortion.FirstHalf));
        Assert.Equal(.5, halfDay.WorkDayCount);
        Assert.Equal(LeaveDayPortion.FirstHalf, halfDay.DayPortion);
    }

    private static MobileLeaveRequestService CreateService(AppDbContext context)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Attendance:TimeZone"] = "Europe/Istanbul"
        }).Build();
        return new MobileLeaveRequestService(
            new LeaveRequestRepository(context), new UnitOfWork(context),
            new WorkCalendarResolver(context),
            new TestTimeProvider(new DateTimeOffset(2026, 7, 14, 8, 0, 0, TimeSpan.Zero)), config);
    }

    private static async Task<Guid> SeedUserAsync(AppDbContext context)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User { Id = Guid.NewGuid(), Name = "Test", Email = "test@faydam.com", RoleId = role.Id, Role = role };
        context.AddRange(role, user);
        await context.SaveChangesAsync();
        return user.Id;
    }
}

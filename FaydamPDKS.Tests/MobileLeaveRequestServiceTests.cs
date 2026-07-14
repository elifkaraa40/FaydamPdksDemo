using FaydamPDKS.Api;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.Extensions.Configuration;
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            userId,
            new CreateLeaveRequestDto(LeaveType.Excuse, new DateOnly(2026, 7, 24), new DateOnly(2026, 7, 25), null)));
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

    private static MobileLeaveRequestService CreateService(AppDbContext context)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Attendance:TimeZone"] = "Europe/Istanbul"
        }).Build();
        return new MobileLeaveRequestService(
            new LeaveRequestRepository(context), new UnitOfWork(context),
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

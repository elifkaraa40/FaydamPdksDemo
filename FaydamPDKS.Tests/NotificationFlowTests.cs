using FaydamPDKS.Api;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using FaydamPDKS.Web;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class NotificationFlowTests
{
    [Fact]
    public async Task Leave_approval_creates_user_notification_and_user_can_mark_it_read()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User { Id = Guid.NewGuid(), Name = "Personel", Email = "personel@faydam.com", RoleId = role.Id, Role = role };
        var reviewer = new User { Id = Guid.NewGuid(), Name = "Yönetici", Email = "yonetici@faydam.com", RoleId = role.Id, Role = role };
        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(), UserId = user.Id, User = user, LeaveType = LeaveType.Annual,
            StartDate = new DateOnly(2026, 7, 20), EndDate = new DateOnly(2026, 7, 21),
            Status = LeaveRequestStatus.Pending, CreatedAt = new DateTimeOffset(2026, 7, 14, 8, 0, 0, TimeSpan.Zero)
        };
        context.AddRange(role, user, reviewer, leave);
        await context.SaveChangesAsync();
        var time = new TestTimeProvider(new DateTimeOffset(2026, 7, 14, 9, 0, 0, TimeSpan.Zero));
        var notificationRepository = new NotificationRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var approval = new WebLeaveApprovalService(new LeaveRequestRepository(context), notificationRepository, new AuditTrail(context, time), unitOfWork, new WorkCalendarResolver(context), time);

        Assert.True(await approval.ReviewAsync(leave.Id, reviewer.Id, new ReviewLeaveRequestDto(true, "Uygundur")));
        var stored = await context.Notifications.SingleAsync();
        Assert.Equal(NotificationType.LeaveApproved, stored.Type);
        Assert.Equal(user.Id, stored.UserId);
        var audit = await context.AuditLogs.SingleAsync();
        Assert.Equal("LeaveRequest.Approved", audit.Action);
        Assert.Equal(reviewer.Id, audit.ActorUserId);

        var mobile = new MobileNotificationService(notificationRepository, unitOfWork, time);
        Assert.False(await mobile.MarkReadAsync(reviewer.Id, stored.Id));
        Assert.True(await mobile.MarkReadAsync(user.Id, stored.Id));
        Assert.NotNull((await context.Notifications.SingleAsync()).ReadAt);
    }
}

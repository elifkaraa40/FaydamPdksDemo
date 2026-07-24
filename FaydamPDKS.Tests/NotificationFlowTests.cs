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

        var mobile = new MobileNotificationService(notificationRepository, unitOfWork, time, context);
        Assert.Equal(1, await mobile.GetUnreadCountAsync(user.Id));
        var english = await mobile.GetMineAsync(user.Id, "en");
        Assert.Equal("Leave request approved", english.Single().Title);
        Assert.False(await mobile.MarkReadAsync(reviewer.Id, stored.Id));
        Assert.True(await mobile.MarkReadAsync(user.Id, stored.Id));
        Assert.NotNull((await context.Notifications.SingleAsync()).ReadAt);
        Assert.Equal(0, await mobile.GetUnreadCountAsync(user.Id));
    }

    [Fact]
    public async Task Push_token_is_linked_to_active_session_and_duplicate_registration_is_moved()
    {
        await using var context = TestInfrastructure.CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = "Personel", NormalizedName = "PERSONEL" };
        var user = new User { Id = Guid.NewGuid(), Name = "Personel", Email = "push@faydam.com", RoleId = role.Id, Role = role };
        var first = new DeviceSession
        {
            UserId = user.Id, User = user, DeviceIdHash = new string('A', 64), DeviceName = "Telefon A",
            LoggedInAt = DateTimeOffset.UtcNow, LastActiveAt = DateTimeOffset.UtcNow
        };
        var second = new DeviceSession
        {
            UserId = user.Id, User = user, DeviceIdHash = new string('B', 64), DeviceName = "Telefon B",
            LoggedInAt = DateTimeOffset.UtcNow, LastActiveAt = DateTimeOffset.UtcNow
        };
        context.AddRange(role, user, first, second);
        await context.SaveChangesAsync();
        var time = new TestTimeProvider(new DateTimeOffset(2026, 7, 24, 8, 0, 0, TimeSpan.Zero));
        var mobile = new MobileNotificationService(
            new NotificationRepository(context), new UnitOfWork(context), time, context);

        await mobile.RegisterPushDeviceAsync(
            user.Id, first.Id, new RegisterPushDeviceDto("same-fcm-token", "android", "tr"));
        await mobile.RegisterPushDeviceAsync(
            user.Id, second.Id, new RegisterPushDeviceDto("same-fcm-token", "android", "en"));

        Assert.Null(first.PushToken);
        Assert.Equal("same-fcm-token", second.PushToken);
        Assert.Equal("en", second.PushLanguage);
        Assert.NotNull(first.PushTokenDisabledAt);

        await mobile.UnregisterPushDeviceAsync(user.Id, second.Id);
        Assert.Null(second.PushToken);
        Assert.NotNull(second.PushTokenDisabledAt);
    }
}

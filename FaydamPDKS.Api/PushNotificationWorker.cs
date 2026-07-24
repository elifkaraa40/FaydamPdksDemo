using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Api;

public sealed class PushNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IFirebasePushSender sender,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<PushNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = Math.Clamp(configuration.GetValue("Firebase:PollingSeconds", 10), 5, 300);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (sender.IsAvailable) await DispatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Push bildirim kuyruğu işlenemedi.");
            }
            await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
        }
    }

    private async Task DispatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = timeProvider.GetUtcNow();

        var newDeliveries = await (
            from notification in context.Notifications.AsNoTracking()
            from session in context.DeviceSessions.AsNoTracking()
            where session.UserId == notification.UserId
                && session.RevokedAt == null
                && session.PushToken != null
                && session.PushTokenUpdatedAt != null
                && notification.CreatedAt >= session.PushTokenUpdatedAt
                && !context.PushNotificationDeliveries.Any(x =>
                    x.NotificationId == notification.Id && x.DeviceSessionId == session.Id)
            orderby notification.CreatedAt
            select new { NotificationId = notification.Id, DeviceSessionId = session.Id })
            .Take(100)
            .ToArrayAsync(cancellationToken);

        foreach (var item in newDeliveries)
        {
            context.PushNotificationDeliveries.Add(new PushNotificationDelivery
            {
                NotificationId = item.NotificationId,
                DeviceSessionId = item.DeviceSessionId,
                CreatedAt = now,
                NextAttemptAt = now
            });
        }
        if (newDeliveries.Length > 0) await context.SaveChangesAsync(cancellationToken);

        var pending = await context.PushNotificationDeliveries
            .Include(x => x.Notification)
            .Include(x => x.DeviceSession)
            .Where(x => x.SentAt == null
                && (x.NextAttemptAt == null || x.NextAttemptAt <= now)
                && x.DeviceSession.RevokedAt == null
                && x.DeviceSession.PushToken != null)
            .OrderBy(x => x.CreatedAt)
            .Take(50)
            .ToArrayAsync(cancellationToken);

        foreach (var delivery in pending)
        {
            var localized = PushNotificationText.Create(
                delivery.Notification,
                delivery.DeviceSession.PushLanguage);
            var result = await sender.SendAsync(new PushMessage(
                delivery.DeviceSession.PushToken!,
                localized.Title,
                localized.Message,
                new Dictionary<string, string>
                {
                    ["eventId"] = delivery.NotificationId.ToString(),
                    ["notificationId"] = delivery.NotificationId.ToString(),
                    ["type"] = delivery.Notification.Type.ToString(),
                    ["route"] = localized.Route,
                    ["relatedEntityId"] = delivery.Notification.RelatedEntityId?.ToString() ?? string.Empty
                }), cancellationToken);

            delivery.AttemptCount++;
            delivery.LastAttemptAt = now;
            delivery.ErrorCode = result.ErrorCode;
            if (result.Success)
            {
                delivery.SentAt = now;
                delivery.NextAttemptAt = null;
            }
            else if (result.InvalidToken)
            {
                delivery.DeviceSession.PushToken = null;
                delivery.DeviceSession.PushTokenDisabledAt = now;
                delivery.NextAttemptAt = null;
            }
            else
            {
                var retryMinutes = Math.Min(60, Math.Pow(2, Math.Min(delivery.AttemptCount - 1, 6)));
                delivery.NextAttemptAt = now.AddMinutes(retryMinutes);
            }
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

internal sealed record LocalizedPushText(string Title, string Message, string Route);

internal static class PushNotificationText
{
    public static LocalizedPushText Create(Notification notification, string? language)
    {
        var route = notification.Type switch
        {
            NotificationType.LeaveApproved or NotificationType.LeaveRejected => "leave_requests",
            NotificationType.AttendanceCorrectionApproved or NotificationType.AttendanceCorrectionRejected => "attendance_corrections",
            NotificationType.WorkLocationAssigned or NotificationType.FieldWorkRequestApproved or NotificationType.FieldWorkRequestRejected => "work_locations",
            NotificationType.LeaveRequestCreated or NotificationType.AttendanceCorrectionCreated or NotificationType.FieldWorkRequestCreated or NotificationType.RegistrationApprovalRequested => "manager_approvals",
            _ => "notifications"
        };
        if (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            return new(notification.Title, notification.Message, route);

        return notification.Type switch
        {
            NotificationType.LeaveApproved => new("Leave request approved", "Your leave request has been approved.", route),
            NotificationType.LeaveRejected => new("Leave request rejected", "Your leave request has been rejected.", route),
            NotificationType.AttendanceCorrectionApproved => new("Attendance correction approved", "Your attendance correction request has been approved.", route),
            NotificationType.AttendanceCorrectionRejected => new("Attendance correction rejected", "Your attendance correction request has been rejected.", route),
            NotificationType.FieldWorkRequestApproved or NotificationType.WorkLocationAssigned => new("Work location approved", "Your work location request has been approved.", route),
            NotificationType.FieldWorkRequestRejected => new("Work location rejected", "Your work location request has been rejected.", route),
            NotificationType.LeaveRequestCreated => new("New leave request", "A new leave request is waiting for approval.", route),
            NotificationType.AttendanceCorrectionCreated => new("New attendance correction", "A new attendance correction is waiting for approval.", route),
            NotificationType.FieldWorkRequestCreated => new("New work location request", "A new work location request is waiting for approval.", route),
            NotificationType.RegistrationApprovalRequested => new("New registration request", "A new account registration is waiting for approval.", route),
            NotificationType.RegistrationApproved => new("Account approved", "Your account has been approved.", "notifications"),
            NotificationType.RegistrationRejected => new("Account rejected", "Your account registration has been rejected.", "notifications"),
            _ => new("Faydam PDKS notification", "You have a new notification.", route)
        };
    }
}

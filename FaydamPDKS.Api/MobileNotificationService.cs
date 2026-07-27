using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Api;

public sealed class MobileNotificationService(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    AppDbContext context) : IMobileNotificationService
{
    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(
        Guid userId,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var items = await notifications.GetForUserAsync(userId, 100, cancellationToken);
        var decidedLeaveIds = items
            .Where(x => x.RelatedEntityId.HasValue
                && x.Type is NotificationType.LeaveApproved or NotificationType.LeaveRejected)
            .Select(x => x.RelatedEntityId!.Value)
            .ToHashSet();
        return items
            .Where(x => !(x.Type == NotificationType.LeaveRequestCreated
                && x.RelatedEntityId.HasValue
                && decidedLeaveIds.Contains(x.RelatedEntityId.Value)))
            .Select(x => Map(x, language))
            .ToArray();
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Notifications.CountAsync(x => x.UserId == userId && x.ReadAt == null, cancellationToken);

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await notifications.GetForUserByIdAsync(userId, notificationId, cancellationToken);
        if (notification is null) return false;
        if (!notification.IsRead)
        {
            notification.ReadAt = timeProvider.GetUtcNow();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    public async Task RegisterPushDeviceAsync(
        Guid userId,
        Guid sessionId,
        RegisterPushDeviceDto request,
        CancellationToken cancellationToken = default)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 2048)
            throw new ArgumentException("Geçerli bir bildirim cihaz anahtarı gönderilmelidir.");

        var platform = request.Platform?.Trim().ToLowerInvariant();
        if (platform is not "android" and not "ios")
            throw new ArgumentException("Bildirim platformu android veya ios olmalıdır.");

        var session = await context.DeviceSessions.SingleOrDefaultAsync(
            x => x.Id == sessionId && x.UserId == userId && x.RevokedAt == null,
            cancellationToken) ?? throw new InvalidOperationException("Aktif cihaz oturumu bulunamadı.");

        var duplicateSessions = await context.DeviceSessions
            .Where(x => x.Id != sessionId && x.PushToken == token)
            .ToListAsync(cancellationToken);
        foreach (var duplicate in duplicateSessions)
        {
            duplicate.PushToken = null;
            duplicate.PushTokenDisabledAt = timeProvider.GetUtcNow();
        }

        var tokenChanged = !string.Equals(session.PushToken, token, StringComparison.Ordinal);
        session.PushToken = token;
        session.PushPlatform = platform;
        session.PushLanguage = request.Language?.Equals("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "tr";
        if (tokenChanged || session.PushTokenUpdatedAt is null)
            session.PushTokenUpdatedAt = timeProvider.GetUtcNow();
        session.PushTokenDisabledAt = null;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnregisterPushDeviceAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await context.DeviceSessions.SingleOrDefaultAsync(
            x => x.Id == sessionId && x.UserId == userId,
            cancellationToken);
        if (session is null) return;
        session.PushToken = null;
        session.PushTokenDisabledAt = timeProvider.GetUtcNow();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static NotificationDto Map(Notification x, string? language)
    {
        var localized = PushNotificationText.Create(x, language);
        return new(
            x.Id, x.Type, localized.Title, localized.Message,
            x.RelatedEntityId, x.CreatedAt, x.ReadAt, x.IsRead);
    }
}

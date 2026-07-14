using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Api;

public sealed class MobileNotificationService(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IMobileNotificationService
{
    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await notifications.GetForUserAsync(userId, 100, cancellationToken)).Select(Map).ToArray();

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

    private static NotificationDto Map(Notification x) => new(
        x.Id, x.Type, x.Title, x.Message, x.RelatedEntityId, x.CreatedAt, x.ReadAt, x.IsRead);
}

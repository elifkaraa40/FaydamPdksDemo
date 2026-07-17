using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class ManagerNotificationService(AppDbContext context, TimeProvider timeProvider) : IManagerNotificationService
{
    public async Task NotifyAsync(NotificationType type, string title, string message, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var managerIds = await context.Users.AsNoTracking()
            .Where(x => x.IsActive && x.Role != null && x.Role.Name == "Yonetici")
            .Select(x => x.Id).ToArrayAsync(cancellationToken);
        foreach (var managerId in managerIds)
            context.Notifications.Add(new Notification
            {
                UserId = managerId, Type = type, Title = title, Message = message,
                RelatedEntityId = relatedEntityId, CreatedAt = timeProvider.GetUtcNow()
            });
    }
}

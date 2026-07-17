using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.Interfaces;

public interface IManagerNotificationService
{
    Task NotifyAsync(NotificationType type, string title, string message, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);
}

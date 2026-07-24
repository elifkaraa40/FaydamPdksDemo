using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IMobileNotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetMineAsync(Guid userId, string? language = null, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task RegisterPushDeviceAsync(Guid userId, Guid sessionId, RegisterPushDeviceDto request, CancellationToken cancellationToken = default);
    Task UnregisterPushDeviceAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
}

using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IAuditTrail
{
    Task RecordAsync(Guid? actorUserId, string action, string entityType, string entityId, object? oldValues, object? newValues, string? correlationId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogListItemDto>> GetRecentAsync(int limit = 200, CancellationToken cancellationToken = default);
}

using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.DTOs;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    Guid? RelatedEntityId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    bool IsRead);

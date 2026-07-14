namespace FaydamPDKS.Core.DTOs;

public sealed record AuditLogListItemDto(Guid Id, string ActorName, string Action, string EntityType, string EntityId, DateTimeOffset OccurredAt, string? OldValuesJson, string? NewValuesJson, string? CorrelationId);

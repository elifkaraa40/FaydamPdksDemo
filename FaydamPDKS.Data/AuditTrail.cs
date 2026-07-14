using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FaydamPDKS.Data;

public sealed class AuditTrail(AppDbContext context, TimeProvider timeProvider) : IAuditTrail
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RecordAsync(Guid? actorUserId, string action, string entityType, string entityId, object? oldValues, object? newValues, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        await context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = actorUserId, Action = action, EntityType = entityType,
            EntityId = entityId, OldValuesJson = Serialize(oldValues), NewValuesJson = Serialize(newValues),
            OccurredAt = timeProvider.GetUtcNow(), CorrelationId = correlationId
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogListItemDto>> GetRecentAsync(int limit = 200, CancellationToken cancellationToken = default) =>
        await context.AuditLogs.AsNoTracking().Include(x => x.ActorUser).OrderByDescending(x => x.OccurredAt)
            .Take(Math.Clamp(limit, 1, 500)).Select(x => new AuditLogListItemDto(x.Id,
                x.ActorUser == null ? "Sistem" : x.ActorUser.Name, x.Action, x.EntityType, x.EntityId,
                x.OccurredAt, x.OldValuesJson, x.NewValuesJson, x.CorrelationId)).ToListAsync(cancellationToken);

    private static string? Serialize(object? value) => value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}

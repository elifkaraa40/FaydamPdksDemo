using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class BreakService(AppDbContext context, TimeProvider timeProvider) : IBreakService
{
    public async Task<CurrentBreakDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var active = await context.BreakRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.EndedAt == null, cancellationToken);
        return active is null ? new(false, null, null) : new(true, active.Id, active.StartedAt);
    }

    public async Task<CurrentBreakDto> StartAsync(Guid userId, string deviceEventId, CancellationToken cancellationToken = default)
    {
        if (!await context.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("ACTIVE_USER_NOT_FOUND");
        if (await context.BreakRecords.AnyAsync(x => x.UserId == userId && x.EndedAt == null, cancellationToken))
            throw new InvalidOperationException("BREAK_ALREADY_ACTIVE");

        var activeAttendanceThreshold = timeProvider.GetUtcNow().AddHours(-24).UtcDateTime;
        var lastTransition = await context.AccessLogs.AsNoTracking()
            .Where(x => x.UserId == userId && x.LogDate >= activeAttendanceThreshold)
            .OrderByDescending(x => x.LogDate).Select(x => x.LogType).FirstOrDefaultAsync(cancellationToken);
        if (!string.Equals(lastTransition, "Giris", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("BREAK_REQUIRES_ACTIVE_ATTENDANCE");

        var normalizedEventId = NormalizeEventId(deviceEventId);
        if (await DeviceEventExistsAsync(normalizedEventId, cancellationToken))
            throw new InvalidOperationException("DUPLICATE_EVENT");

        var entity = new BreakRecord
        {
            UserId = userId,
            StartedAt = timeProvider.GetUtcNow(),
            StartDeviceEventId = normalizedEventId
        };
        context.BreakRecords.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new(true, entity.Id, entity.StartedAt);
    }

    public async Task<CurrentBreakDto> EndAsync(Guid userId, Guid breakId, string deviceEventId, CancellationToken cancellationToken = default)
    {
        var entity = await context.BreakRecords.SingleOrDefaultAsync(
            x => x.Id == breakId && x.UserId == userId && x.EndedAt == null, cancellationToken)
            ?? throw new InvalidOperationException("ACTIVE_BREAK_NOT_FOUND");
        var normalizedEventId = NormalizeEventId(deviceEventId);
        if (await DeviceEventExistsAsync(normalizedEventId, cancellationToken))
            throw new InvalidOperationException("DUPLICATE_EVENT");
        entity.EndedAt = timeProvider.GetUtcNow();
        entity.EndDeviceEventId = normalizedEventId;
        await context.SaveChangesAsync(cancellationToken);
        return new(false, null, null);
    }

    public async Task<IReadOnlyList<BreakHistoryItemDto>> GetHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (from > to || to.DayNumber - from.DayNumber > 90) throw new ArgumentException("Geçersiz tarih aralığı.");
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        return await context.BreakRecords.AsNoTracking()
            .Where(x => x.UserId == userId && x.StartedAt >= start && x.StartedAt < end)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new BreakHistoryItemDto(x.Id, x.StartedAt, x.EndedAt,
                x.EndedAt.HasValue ? (int?)(x.EndedAt.Value - x.StartedAt).TotalMinutes : null, x.AutoClosed))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveColleagueBreakDto>> GetActiveColleaguesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return [];
        return await context.BreakRecords.AsNoTracking()
            .Where(x => x.EndedAt == null && x.UserId != userId)
            .Where(x => user.WorkplaceId != null && x.User.WorkplaceId == user.WorkplaceId)
            .OrderBy(x => x.StartedAt)
            .Select(x => new ActiveColleagueBreakDto(x.UserId, x.User.Name,
                x.User.Department != null ? x.User.Department.Name : x.User.DepartmentLegacy, x.StartedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<int?> GetCompletedMinutesAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var records = await context.BreakRecords.AsNoTracking()
            .Where(x => x.UserId == userId && x.EndedAt.HasValue && x.StartedAt >= from && x.StartedAt < to)
            .Select(x => new { x.StartedAt, x.EndedAt })
            .ToArrayAsync(cancellationToken);
        if (records.Length == 0) return null;
        return records.Sum(x => Math.Max(0, (int)(x.EndedAt!.Value - x.StartedAt).TotalMinutes));
    }

    private Task<bool> DeviceEventExistsAsync(string value, CancellationToken cancellationToken) =>
        context.BreakRecords.AnyAsync(x => x.StartDeviceEventId == value || x.EndDeviceEventId == value, cancellationToken);

    private static string NormalizeEventId(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Cihaz olay kimliği zorunludur.") : value.Trim();
}

using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class AccessLogRepository(AppDbContext context) : Repository<AccessLog>(context), IAccessLogRepository
{
    public Task<bool> DeviceEventExistsAsync(string deviceEventId, CancellationToken cancellationToken = default) =>
        Context.AccessLogs.AnyAsync(x => x.DeviceEventId == deviceEventId, cancellationToken);

    public async Task<IReadOnlyList<AccessLog>> GetForUserAsync(
        Guid userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) =>
        await Context.AccessLogs.AsNoTracking()
            .Where(x => x.UserId == userId && x.LogDate >= fromUtc && x.LogDate < toUtc)
            .OrderBy(x => x.LogDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AccessLog>> GetRecentQrForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await Context.AccessLogs.AsNoTracking()
            .Where(x => x.UserId == userId && x.Source == "MobileQr")
            .OrderByDescending(x => x.LogDate)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
}

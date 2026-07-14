using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class NotificationRepository(AppDbContext context) : Repository<Notification>(context), INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, int take, CancellationToken cancellationToken = default) =>
        await Context.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ReadAt.HasValue)
            .ThenByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

    public Task<Notification?> GetForUserByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default) =>
        Context.Notifications.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);
}

using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class AttendanceCorrectionRepository(AppDbContext context) : IAttendanceCorrectionRepository
{
    public async Task<IReadOnlyList<AttendanceCorrectionRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.AttendanceCorrectionRequests.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.WorkDate).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<AttendanceCorrectionRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.AttendanceCorrectionRequests.AsNoTracking().Include(x => x.User).OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    public Task<AttendanceCorrectionRequest?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<AttendanceCorrectionRequest> query = context.AttendanceCorrectionRequests.Include(x => x.User);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
    public Task<bool> HasPendingAsync(Guid userId, DateOnly workDate, CancellationToken cancellationToken = default) =>
        context.AttendanceCorrectionRequests.AnyAsync(x => x.UserId == userId && x.WorkDate == workDate && x.Status == AttendanceCorrectionStatus.Pending, cancellationToken);
    public async Task<IReadOnlyList<AttendanceCorrectionRequest>> GetApprovedAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await context.AttendanceCorrectionRequests.AsNoTracking().Where(x => x.UserId == userId && x.WorkDate >= from && x.WorkDate <= to && x.Status == AttendanceCorrectionStatus.Approved)
            .OrderByDescending(x => x.ReviewedAt).ToListAsync(cancellationToken);
    public async Task AddAsync(AttendanceCorrectionRequest entity, CancellationToken cancellationToken = default) => await context.AttendanceCorrectionRequests.AddAsync(entity, cancellationToken);
}

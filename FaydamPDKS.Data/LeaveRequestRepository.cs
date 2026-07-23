using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class LeaveRequestRepository(AppDbContext context) : Repository<LeaveRequest>(context), ILeaveRequestRepository
{
    public async Task<IReadOnlyList<LeaveRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Context.LeaveRequests.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<LeaveRequest?> GetForUserByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default) =>
        Context.LeaveRequests.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

    public Task<bool> HasActiveOverlapAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
        Context.LeaveRequests.AnyAsync(x =>
            x.UserId == userId &&
            (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved) &&
            x.StartDate <= endDate && x.EndDate >= startDate,
            cancellationToken);

    public Task<LeaveRequest?> FindActiveOverlapAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        Context.LeaveRequests.AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && (x.Status == LeaveRequestStatus.Pending
                    || x.Status == LeaveRequestStatus.Approved)
                && x.StartDate <= endDate
                && x.EndDate >= startDate)
            .OrderBy(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<LeaveRequest>> GetAllWithUsersAsync(CancellationToken cancellationToken = default) =>
        await Context.LeaveRequests.AsNoTracking().Include(x => x.User)
            .OrderBy(x => x.Status == LeaveRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<LeaveRequest?> GetByIdWithUserAsync(Guid id, bool asTracking, CancellationToken cancellationToken = default)
    {
        IQueryable<LeaveRequest> query = Context.LeaveRequests.Include(x => x.User);
        if (!asTracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}

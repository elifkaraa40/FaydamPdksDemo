using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface ILeaveRequestRepository : IRepository<LeaveRequest>
{
    Task<IReadOnlyList<LeaveRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetForUserByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveOverlapAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> FindActiveOverlapAsync(Guid userId, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetAllWithUsersAsync(CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetByIdWithUserAsync(Guid id, bool asTracking, CancellationToken cancellationToken = default);
}

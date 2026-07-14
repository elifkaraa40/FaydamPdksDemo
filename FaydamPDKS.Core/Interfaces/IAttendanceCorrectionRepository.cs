using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface IAttendanceCorrectionRepository
{
    Task<IReadOnlyList<AttendanceCorrectionRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceCorrectionRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AttendanceCorrectionRequest?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> HasPendingAsync(Guid userId, DateOnly workDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceCorrectionRequest>> GetApprovedAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task AddAsync(AttendanceCorrectionRequest entity, CancellationToken cancellationToken = default);
}

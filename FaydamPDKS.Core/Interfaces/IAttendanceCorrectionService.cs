using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IAttendanceCorrectionService
{
    Task<IReadOnlyList<AttendanceCorrectionDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AttendanceCorrectionDto> CreateAsync(Guid userId, CreateAttendanceCorrectionDto request, CancellationToken cancellationToken = default);
}

public interface IWebAttendanceCorrectionService
{
    Task<IReadOnlyList<AttendanceCorrectionReviewDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ReviewAsync(Guid id, Guid reviewerId, ReviewAttendanceCorrectionDto request, string? correlationId, CancellationToken cancellationToken = default);
}

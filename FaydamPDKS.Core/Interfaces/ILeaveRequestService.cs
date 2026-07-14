using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface ILeaveRequestService
{
    Task<IReadOnlyList<LeaveRequestDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> CreateAsync(Guid userId, CreateLeaveRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid userId, Guid requestId, CancellationToken cancellationToken = default);
}

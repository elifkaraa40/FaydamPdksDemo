using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IWebLeaveApprovalService
{
    Task<IReadOnlyList<LeaveReviewListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LeaveReviewListItemDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ReviewAsync(Guid id, Guid reviewerUserId, ReviewLeaveRequestDto request, CancellationToken cancellationToken = default);
}

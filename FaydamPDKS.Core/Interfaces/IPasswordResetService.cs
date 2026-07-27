using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IPasswordResetService
{
    Task<PasswordResetEmailTicket?> CreateEmailResetAsync(string email, CancellationToken cancellationToken = default);
    Task RequestManagerResetAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ResetWithTokenAsync(string rawToken, string newPassword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PasswordResetRequestListItemDto>> GetPendingManagerRequestsAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task<PasswordResetReviewResult> ReviewManagerRequestAsync(Guid requestId, Guid managerId, bool approve, string? note, string? correlationId = null, CancellationToken cancellationToken = default);
}

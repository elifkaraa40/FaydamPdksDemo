using FaydamPDKS.Core.DTOs.Auth;

namespace FaydamPDKS.Core.Interfaces;

public interface IMobileAuthService
{
    Task<MobileAuthResponse?> LoginAsync(MobileLoginRequest request, CancellationToken cancellationToken = default);
    Task<MobileAuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default);
}

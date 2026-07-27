using FaydamPDKS.Core.DTOs.Auth;

namespace FaydamPDKS.Core.Interfaces;

public interface IMobileAuthService
{
    Task<MobileAuthResponse?> LoginAsync(MobileLoginRequest request, CancellationToken cancellationToken = default);
    Task<MobileAuthResponse?> RefreshAsync(string refreshToken, string deviceId, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceSessionDto>> GetDeviceSessionsAsync(Guid userId, Guid? currentSessionId,
        CancellationToken cancellationToken = default);
    Task<DeviceSessionValidationResult> ValidateDeviceSessionAsync(Guid userId, Guid sessionId,
        string deviceId, CancellationToken cancellationToken = default);
}

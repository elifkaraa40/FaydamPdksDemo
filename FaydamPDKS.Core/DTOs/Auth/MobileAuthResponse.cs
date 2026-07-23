namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record MobileAuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    MobileUserDto User,
    Guid DeviceSessionId,
    bool PreviousDeviceSessionRevoked = false,
    string? DeviceSessionNotice = null);

public sealed record MobileUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string? ProfileImageUrl,
    string AccountStatus = "Active",
    string? PhoneNumber = null);

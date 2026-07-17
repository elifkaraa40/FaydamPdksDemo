namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record MobileAuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    MobileUserDto User);

public sealed record MobileUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string? ProfileImageUrl,
    string AccountStatus = "Active",
    string? PhoneNumber = null);

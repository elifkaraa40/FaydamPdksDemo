namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record DeviceSessionDto(
    Guid Id,
    string DeviceName,
    string DeviceIdentifier,
    DateTimeOffset LoggedInAt,
    DateTimeOffset LastActiveAt,
    DateTimeOffset? RevokedAt,
    bool IsCurrentDevice);

public sealed record DeviceSessionValidationResult(bool IsValid, string? ErrorCode = null);

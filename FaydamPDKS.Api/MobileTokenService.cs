using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FaydamPDKS.Api;

public sealed class MobileTokenService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    AppDbContext context,
    IConfiguration configuration,
    TimeProvider timeProvider) : IMobileAuthService
{
    public async Task<MobileAuthResponse?> LoginAsync(
        MobileLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByEmailWithRoleAsync(request.Email.Trim().ToUpperInvariant(), cancellationToken);
        if (user is null || !user.IsActive || user.AccountStatus != AccountStatus.Active
            || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;

        var (session, previousSessionRevoked) = await OpenDeviceSessionAsync(
            user, request.DeviceId, request.DeviceName, cancellationToken);
        var issued = await IssueTokenPairAsync(user, session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return issued.Response with
        {
            PreviousDeviceSessionRevoked = previousSessionRevoked,
            DeviceSessionNotice = previousSessionRevoked
                ? "Bu hesap başka bir cihazda açıktı. Önceki cihaz oturumu güvenliğiniz için kapatıldı."
                : null
        };
    }

    public async Task<MobileAuthResponse?> RefreshAsync(
        string refreshToken,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(deviceId)) return null;
        var now = timeProvider.GetUtcNow();
        var current = await refreshTokens.GetActiveByHashAsync(Hash(refreshToken), now, cancellationToken);
        if (current?.DeviceSession is null
            || current.DeviceSession.RevokedAt is not null
            || !FixedTimeEquals(current.DeviceSession.DeviceIdHash, Hash(deviceId))
            || !current.User.IsActive
            || current.User.AccountStatus is AccountStatus.Rejected or AccountStatus.Suspended)
            return null;

        current.RevokedAt = now;
        current.DeviceSession.LastActiveAt = now;
        var issued = await IssueTokenPairAsync(current.User, current.DeviceSession, cancellationToken);
        current.ReplacedByTokenId = issued.RefreshTokenEntity.Id;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return issued.Response;
    }

    public async Task RevokeAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var now = timeProvider.GetUtcNow();
        var token = await refreshTokens.GetActiveByHashAsync(Hash(refreshToken), now, cancellationToken);
        if (token is null || token.UserId != userId) return;

        token.RevokedAt = now;
        if (token.DeviceSession is not null)
        {
            token.DeviceSession.RevokedAt = now;
            token.DeviceSession.RevokeReason = "USER_LOGOUT";
            token.DeviceSession.PushToken = null;
            token.DeviceSession.PushTokenDisabledAt = now;
            await RevokeTokensForSessionsAsync([token.DeviceSession.Id], now, cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var sessions = await context.DeviceSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
            session.RevokeReason = "LOGOUT_ALL";
            session.PushToken = null;
            session.PushTokenDisabledAt = now;
        }
        await refreshTokens.RevokeAllForUserAsync(userId, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceSessionDto>> GetDeviceSessionsAsync(
        Guid userId,
        Guid? currentSessionId,
        CancellationToken cancellationToken = default) =>
        await context.DeviceSessions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RevokedAt == null)
            .ThenByDescending(x => x.LastActiveAt)
            .Take(20)
            .Select(x => new DeviceSessionDto(
                x.Id,
                x.DeviceName,
                x.DeviceIdHash.Substring(0, 12),
                x.LoggedInAt,
                x.LastActiveAt,
                x.RevokedAt,
                currentSessionId.HasValue && x.Id == currentSessionId.Value))
            .ToArrayAsync(cancellationToken);

    public async Task<DeviceSessionValidationResult> ValidateDeviceSessionAsync(
        Guid userId,
        Guid sessionId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return new(false, "DEVICE_ID_REQUIRED");

        var session = await context.DeviceSessions
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null || session.RevokedAt is not null)
            return new(false, "DEVICE_SESSION_REVOKED");
        if (!FixedTimeEquals(session.DeviceIdHash, Hash(deviceId)))
            return new(false, "DEVICE_MISMATCH");

        session.LastActiveAt = timeProvider.GetUtcNow();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(true);
    }

    public async Task<MobileAuthResponse> IssueForUserAsync(
        User user,
        string deviceId,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        var (session, previousSessionRevoked) = await OpenDeviceSessionAsync(
            user, deviceId, deviceName, cancellationToken);
        var issued = await IssueTokenPairAsync(user, session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return issued.Response with { PreviousDeviceSessionRevoked = previousSessionRevoked };
    }

    private async Task<(DeviceSession Session, bool PreviousSessionRevoked)> OpenDeviceSessionAsync(
        User user,
        string deviceId,
        string? deviceName,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var deviceIdHash = Hash(deviceId);
        var activeSessions = await context.DeviceSessions
            .Where(x => x.UserId == user.Id && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        var allowMultipleDevices = IsManager(user)
            && configuration.GetValue("Security:AllowManagerMultipleDevices", true);
        var sessionsToRevoke = allowMultipleDevices
            ? activeSessions.Where(x => FixedTimeEquals(x.DeviceIdHash, deviceIdHash)).ToArray()
            : activeSessions.ToArray();

        foreach (var oldSession in sessionsToRevoke)
        {
            oldSession.RevokedAt = now;
            oldSession.RevokeReason = FixedTimeEquals(oldSession.DeviceIdHash, deviceIdHash)
                ? "SESSION_RENEWED"
                : "SIGNED_IN_ON_ANOTHER_DEVICE";
            oldSession.PushToken = null;
            oldSession.PushTokenDisabledAt = now;
        }
        await RevokeTokensForSessionsAsync(sessionsToRevoke.Select(x => x.Id), now, cancellationToken);

        var session = new DeviceSession
        {
            UserId = user.Id,
            DeviceIdHash = deviceIdHash,
            DeviceName = NormalizeDeviceName(deviceName),
            LoggedInAt = now,
            LastActiveAt = now
        };
        context.DeviceSessions.Add(session);
        return (session, sessionsToRevoke.Any(x => !FixedTimeEquals(x.DeviceIdHash, deviceIdHash)));
    }

    private async Task RevokeTokensForSessionsAsync(
        IEnumerable<Guid> sessionIds,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        var ids = sessionIds.Distinct().ToArray();
        if (ids.Length == 0) return;
        var tokens = await context.RefreshTokens
            .Where(x => x.DeviceSessionId.HasValue
                && ids.Contains(x.DeviceSessionId.Value)
                && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens) token.RevokedAt = revokedAt;
    }

    private async Task<(MobileAuthResponse Response, RefreshToken RefreshTokenEntity)> IssueTokenPairAsync(
        User user,
        DeviceSession session,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(GetRequiredInt("Jwt:AccessTokenMinutes"));
        var key = GetRequired("Jwt:Key");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? "Personel"),
            new Claim("account_status", user.AccountStatus.ToString()),
            new Claim("sid", session.Id.ToString()),
            new Claim("device_hash", session.DeviceIdHash),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var jwt = new JwtSecurityToken(
            GetRequired("Jwt:Issuer"), GetRequired("Jwt:Audience"), claims,
            now.UtcDateTime, expiresAt.UtcDateTime,
            new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        var plainRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            DeviceSessionId = session.Id,
            DeviceSession = session,
            TokenHash = Hash(plainRefreshToken),
            DeviceName = session.DeviceName,
            CreatedAt = now,
            ExpiresAt = now.AddDays(GetRequiredInt("Jwt:RefreshTokenDays"))
        };
        await refreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

        var response = new MobileAuthResponse(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            plainRefreshToken,
            expiresAt,
            new MobileUserDto(
                user.Id,
                user.Name,
                user.Email,
                user.Role?.Name ?? "Personel",
                user.ProfileImageUrl,
                user.AccountStatus.ToString(),
                user.PhoneNumber),
            session.Id);
        return (response, refreshTokenEntity);
    }

    private static bool IsManager(User user)
    {
        var role = user.Role?.NormalizedName ?? user.Role?.Name ?? string.Empty;
        return role.Equals("YONETICI", StringComparison.OrdinalIgnoreCase)
            || role.Equals("YÖNETİCİ", StringComparison.OrdinalIgnoreCase)
            || role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDeviceName(string? deviceName) =>
        string.IsNullOrWhiteSpace(deviceName) ? "Mobil cihaz" : deviceName.Trim();

    private string GetRequired(string key) => configuration[key]
        ?? throw new InvalidOperationException($"{key} yapılandırılmalıdır.");

    private int GetRequiredInt(string key) =>
        int.TryParse(configuration[key], out var value) && value > 0
            ? value
            : throw new InvalidOperationException($"{key} pozitif bir sayı olmalıdır.");

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim())));

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(left),
            Encoding.ASCII.GetBytes(right));
}

using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FaydamPDKS.Web;

public sealed class WebDeviceSessionService(
    AppDbContext context,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    public async Task<DeviceSession> OpenAsync(
        User user,
        string deviceId,
        string deviceName,
        CancellationToken cancellationToken = default)
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
        }

        var revokedIds = sessionsToRevoke.Select(x => x.Id).ToArray();
        if (revokedIds.Length > 0)
        {
            var refreshTokens = await context.RefreshTokens
                .Where(x => x.DeviceSessionId.HasValue
                    && revokedIds.Contains(x.DeviceSessionId.Value)
                    && x.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var refreshToken in refreshTokens) refreshToken.RevokedAt = now;
        }

        var session = new DeviceSession
        {
            UserId = user.Id,
            DeviceIdHash = deviceIdHash,
            DeviceName = NormalizeDeviceName(deviceName),
            LoggedInAt = now,
            LastActiveAt = now
        };
        context.DeviceSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<bool> ValidateAndTouchAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await context.DeviceSessions.SingleOrDefaultAsync(
            x => x.Id == sessionId && x.UserId == userId,
            cancellationToken);
        if (session is null || session.RevokedAt is not null) return false;

        var now = timeProvider.GetUtcNow();
        if (now - session.LastActiveAt >= TimeSpan.FromMinutes(1))
        {
            session.LastActiveAt = now;
            await context.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    public async Task RevokeAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await context.DeviceSessions.SingleOrDefaultAsync(
            x => x.Id == sessionId && x.UserId == userId && x.RevokedAt == null,
            cancellationToken);
        if (session is null) return;

        var now = timeProvider.GetUtcNow();
        session.RevokedAt = now;
        session.RevokeReason = "USER_LOGOUT";

        var refreshTokens = await context.RefreshTokens
            .Where(x => x.DeviceSessionId == sessionId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in refreshTokens) refreshToken.RevokedAt = now;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllExceptAsync(
        Guid userId,
        Guid? sessionIdToKeep,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var sessions = await context.DeviceSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null
                && (!sessionIdToKeep.HasValue || x.Id != sessionIdToKeep.Value))
            .ToListAsync(cancellationToken);
        foreach (var session in sessions) { session.RevokedAt = now; session.RevokeReason = reason; }

        var refreshTokens = await context.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null
                && (!sessionIdToKeep.HasValue || x.DeviceSessionId != sessionIdToKeep.Value))
            .ToListAsync(cancellationToken);
        foreach (var token in refreshTokens) token.RevokedAt = now;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsManager(User user)
    {
        var role = user.Role?.NormalizedName ?? user.Role?.Name ?? string.Empty;
        return role.Equals("YONETICI", StringComparison.OrdinalIgnoreCase)
            || role.Equals("YÖNETİCİ", StringComparison.OrdinalIgnoreCase)
            || role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDeviceName(string deviceName) =>
        string.IsNullOrWhiteSpace(deviceName) ? "Web tarayıcısı" : deviceName.Trim()[..Math.Min(deviceName.Trim().Length, 150)];

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim())));

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(left),
            Encoding.ASCII.GetBytes(right));
}

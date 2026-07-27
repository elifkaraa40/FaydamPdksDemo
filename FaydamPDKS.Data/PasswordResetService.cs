using System.Security.Cryptography;
using System.Text;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class PasswordResetService(
    AppDbContext db,
    IAuditTrail audit,
    TimeProvider clock) : IPasswordResetService
{
    public async Task<PasswordResetEmailTicket?> CreateEmailResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await FindActiveUserAsync(email, cancellationToken);
        if (user is null) return null;

        var now = clock.GetUtcNow();
        await ExpireOpenRequestsAsync(user.Id, PasswordResetChannel.Email, now, cancellationToken);
        var rawToken = Base64Url(RandomNumberGenerator.GetBytes(32));
        db.PasswordResetRequests.Add(new PasswordResetRequest
        {
            UserId = user.Id,
            Channel = PasswordResetChannel.Email,
            Status = PasswordResetRequestStatus.EmailSent,
            TokenHash = Hash(rawToken),
            TokenExpiresAt = now.AddMinutes(30),
            CreatedAt = now
        });
        await audit.RecordAsync(null, "PasswordReset.EmailRequested", nameof(User), user.Id.ToString(), null,
            new { Channel = PasswordResetChannel.Email, ExpiresInMinutes = 30 }, cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(user.Email, user.Name, rawToken);
    }

    public async Task RequestManagerResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await FindActiveUserAsync(email, cancellationToken);
        if (user is null) return;
        if (await db.PasswordResetRequests.AnyAsync(x => x.UserId == user.Id
                && x.Channel == PasswordResetChannel.Manager
                && x.Status == PasswordResetRequestStatus.Pending, cancellationToken)) return;

        var now = clock.GetUtcNow();
        var request = new PasswordResetRequest
        {
            UserId = user.Id,
            Channel = PasswordResetChannel.Manager,
            Status = PasswordResetRequestStatus.Pending,
            CreatedAt = now
        };
        db.PasswordResetRequests.Add(request);

        var managers = await db.Users.AsNoTracking()
            .Where(x => x.IsActive && x.AccountStatus == AccountStatus.Active && x.Role != null
                && (x.Role.NormalizedName == "YONETICI" || x.Role.Name == "Yonetici")
                && (user.WorkplaceId == null || x.WorkplaceId == null || x.WorkplaceId == user.WorkplaceId))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);
        foreach (var managerId in managers)
        {
            db.Notifications.Add(new Notification
            {
                UserId = managerId,
                Type = NotificationType.PasswordResetRequested,
                Title = "Şifre sıfırlama talebi",
                Message = $"{user.Name} şifresinin yönetici tarafından sıfırlanmasını istedi.",
                RelatedEntityId = request.Id,
                CreatedAt = now
            });
        }
        await audit.RecordAsync(null, "PasswordReset.ManagerRequested", nameof(User), user.Id.ToString(), null,
            new { Channel = PasswordResetChannel.Manager }, cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ResetWithTokenAsync(
        string rawToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (!ValidPassword(newPassword) || string.IsNullOrWhiteSpace(rawToken)) return false;
        var now = clock.GetUtcNow();
        var tokenHash = Hash(rawToken);
        var request = await db.PasswordResetRequests.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash
                && x.Channel == PasswordResetChannel.Email
                && x.Status == PasswordResetRequestStatus.EmailSent, cancellationToken);
        if (request is null || request.TokenExpiresAt <= now || !request.User.IsActive)
        {
            if (request is not null && request.Status == PasswordResetRequestStatus.EmailSent)
            {
                request.Status = PasswordResetRequestStatus.Expired;
                await db.SaveChangesAsync(cancellationToken);
            }
            return false;
        }
        if (BCrypt.Net.BCrypt.Verify(newPassword, request.User.PasswordHash)) return false;

        request.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        request.User.MustChangePassword = false;
        request.Status = PasswordResetRequestStatus.Completed;
        request.CompletedAt = now;
        request.TokenHash = null;
        await RevokeAllAccessAsync(request.UserId, now, "PASSWORD_RESET", cancellationToken);
        await audit.RecordAsync(request.UserId, "PasswordReset.EmailCompleted", nameof(User), request.UserId.ToString(), null,
            new { SessionsRevoked = true }, cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PasswordResetRequestListItemDto>> GetPendingManagerRequestsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        var manager = await db.Users.AsNoTracking().Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == managerId && x.IsActive, cancellationToken);
        if (manager is null || !IsManager(manager)) throw new UnauthorizedAccessException("Yönetici yetkisi bulunamadı.");

        return await db.PasswordResetRequests.AsNoTracking().Include(x => x.User)
            .Where(x => x.Channel == PasswordResetChannel.Manager
                && x.Status == PasswordResetRequestStatus.Pending
                && x.UserId != managerId
                && (!manager.WorkplaceId.HasValue || !x.User.WorkplaceId.HasValue || x.User.WorkplaceId == manager.WorkplaceId))
            .OrderBy(x => x.CreatedAt)
            .Select(x => new PasswordResetRequestListItemDto(
                x.Id, x.UserId, x.User.Name, x.User.EmployeeNumber, x.User.Email, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PasswordResetReviewResult> ReviewManagerRequestAsync(
        Guid requestId,
        Guid managerId,
        bool approve,
        string? note,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var manager = await db.Users.AsNoTracking().Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == managerId && x.IsActive, cancellationToken);
        if (manager is null || !IsManager(manager)) throw new UnauthorizedAccessException("Yönetici yetkisi bulunamadı.");

        var request = await db.PasswordResetRequests.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == requestId
                && x.Channel == PasswordResetChannel.Manager
                && x.Status == PasswordResetRequestStatus.Pending, cancellationToken);
        if (request is null) return new(false);
        if (request.UserId == managerId) throw new InvalidOperationException("Kendi şifre sıfırlama talebinizi onaylayamazsınız.");
        if (manager.WorkplaceId.HasValue && request.User.WorkplaceId.HasValue
            && manager.WorkplaceId != request.User.WorkplaceId)
            throw new UnauthorizedAccessException("Personel yetki kapsamınızın dışında.");

        var now = clock.GetUtcNow();
        request.ReviewedAt = now;
        request.ReviewedByUserId = managerId;
        request.ReviewNote = Clean(note);
        string? temporaryPassword = null;
        if (approve)
        {
            temporaryPassword = CreateTemporaryPassword();
            request.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            request.User.MustChangePassword = true;
            request.Status = PasswordResetRequestStatus.Approved;
            await RevokeAllAccessAsync(request.UserId, now, "MANAGER_PASSWORD_RESET", cancellationToken);
        }
        else
        {
            request.Status = PasswordResetRequestStatus.Rejected;
        }

        db.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Type = approve ? NotificationType.PasswordResetApproved : NotificationType.PasswordResetRejected,
            Title = approve ? "Şifre sıfırlama talebiniz onaylandı" : "Şifre sıfırlama talebiniz reddedildi",
            Message = approve
                ? "Geçici şifrenizi yöneticinizden alarak giriş yapın ve ilk girişte şifrenizi değiştirin."
                : "Şifre sıfırlama talebiniz reddedildi. Ayrıntı için yöneticinizle iletişime geçin.",
            RelatedEntityId = request.Id,
            CreatedAt = now
        });
        await audit.RecordAsync(managerId, approve ? "PasswordReset.ManagerApproved" : "PasswordReset.ManagerRejected",
            nameof(PasswordResetRequest), request.Id.ToString(), new { Status = PasswordResetRequestStatus.Pending },
            new { request.Status, request.ReviewNote, SessionsRevoked = approve }, correlationId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, temporaryPassword);
    }

    private async Task<User?> FindActiveUserAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await db.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email.ToLower() == normalized
                && x.IsActive && x.AccountStatus == AccountStatus.Active, cancellationToken);
    }

    private async Task ExpireOpenRequestsAsync(Guid userId, PasswordResetChannel channel, DateTimeOffset now, CancellationToken ct)
    {
        var requests = await db.PasswordResetRequests.Where(x => x.UserId == userId && x.Channel == channel
            && (x.Status == PasswordResetRequestStatus.Pending || x.Status == PasswordResetRequestStatus.EmailSent)).ToListAsync(ct);
        foreach (var request in requests)
        {
            request.Status = PasswordResetRequestStatus.Expired;
            request.TokenHash = null;
            request.CompletedAt = now;
        }
    }

    private async Task RevokeAllAccessAsync(Guid userId, DateTimeOffset now, string reason, CancellationToken ct)
    {
        var sessions = await db.DeviceSessions.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(ct);
        foreach (var session in sessions) { session.RevokedAt = now; session.RevokeReason = reason; }
        var refreshTokens = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(ct);
        foreach (var token in refreshTokens) token.RevokedAt = now;
    }

    private static bool IsManager(User user)
    {
        var role = user.Role?.NormalizedName ?? user.Role?.Name ?? string.Empty;
        return role.Equals("YONETICI", StringComparison.OrdinalIgnoreCase)
            || role.Equals("YÖNETİCİ", StringComparison.OrdinalIgnoreCase)
            || role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ValidPassword(string value) =>
        value.Length is >= PasswordPolicy.MinimumLength and <= PasswordPolicy.MaximumLength;

    private static string CreateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var characters = new[] { 'A', 'a', '7', '!' }
            .Concat(RandomNumberGenerator.GetBytes(10).Select(x => alphabet[x % alphabet.Length]))
            .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
            .ToArray();
        return new string(characters);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim())));

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

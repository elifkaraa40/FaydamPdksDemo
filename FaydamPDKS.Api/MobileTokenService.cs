using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Core.Enums;
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
    IConfiguration configuration,
    TimeProvider timeProvider) : IMobileAuthService
{
    public async Task<MobileAuthResponse?> LoginAsync(MobileLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByEmailWithRoleAsync(request.Email.Trim().ToUpperInvariant(), cancellationToken);
        if (user is null || !user.IsActive || user.AccountStatus != AccountStatus.Active
            || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;
        var issued = await IssueTokenPairAsync(user, request.DeviceName, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return issued.Response;
    }

    public async Task<MobileAuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;
        var now = timeProvider.GetUtcNow();
        var current = await refreshTokens.GetActiveByHashAsync(Hash(refreshToken), now, cancellationToken);
        if (current is null) return null;
        if (!current.User.IsActive || current.User.AccountStatus is AccountStatus.Rejected or AccountStatus.Suspended) return null;

        current.RevokedAt = now;
        var issued = await IssueTokenPairAsync(current.User, current.DeviceName, cancellationToken);
        current.ReplacedByTokenId = issued.RefreshTokenEntity.Id;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return issued.Response;
    }

    public async Task RevokeAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await refreshTokens.GetActiveByHashAsync(Hash(refreshToken), timeProvider.GetUtcNow(), cancellationToken);
        if (token is null || token.UserId != userId) return;
        token.RevokedAt = timeProvider.GetUtcNow();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MobileAuthResponse> IssueForUserAsync(User user, string? deviceName, CancellationToken cancellationToken = default)
    {
        var issued = await IssueTokenPairAsync(user, deviceName, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return issued.Response;
    }

    private async Task<(MobileAuthResponse Response, RefreshToken RefreshTokenEntity)> IssueTokenPairAsync(
        User user,
        string? deviceName,
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
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var jwt = new JwtSecurityToken(
            GetRequired("Jwt:Issuer"), GetRequired("Jwt:Audience"), claims,
            now.UtcDateTime, expiresAt.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));

        var plainRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(plainRefreshToken),
            DeviceName = deviceName?.Trim(),
            CreatedAt = now,
            ExpiresAt = now.AddDays(GetRequiredInt("Jwt:RefreshTokenDays"))
        };
        await refreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

        var response = new MobileAuthResponse(
            new JwtSecurityTokenHandler().WriteToken(jwt), plainRefreshToken, expiresAt,
            new MobileUserDto(user.Id, user.Name, user.Email, user.Role?.Name ?? "Personel", user.ProfileImageUrl,
                user.AccountStatus.ToString(), user.PhoneNumber));
        return (response, refreshTokenEntity);
    }

    private string GetRequired(string key) => configuration[key]
        ?? throw new InvalidOperationException($"{key} yapılandırılmalıdır.");

    private int GetRequiredInt(string key) => int.TryParse(configuration[key], out var value) && value > 0
        ? value : throw new InvalidOperationException($"{key} pozitif bir sayı olmalıdır.");

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class RefreshTokenRepository(AppDbContext context) : Repository<RefreshToken>(context), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Context.RefreshTokens.Include(x => x.User).ThenInclude(x => x.Role).Include(x => x.DeviceSession)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAt == null && x.ExpiresAt > now, cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        var active = await Context.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in active) token.RevokedAt = revokedAt;
    }
}

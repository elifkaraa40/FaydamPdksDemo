using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);
}

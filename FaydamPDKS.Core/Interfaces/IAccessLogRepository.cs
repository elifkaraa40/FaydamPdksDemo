using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface IAccessLogRepository : IRepository<AccessLog>
{
    Task<bool> DeviceEventExistsAsync(string deviceEventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessLog>> GetForUserAsync(
        Guid userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}

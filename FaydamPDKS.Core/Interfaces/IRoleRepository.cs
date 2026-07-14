using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

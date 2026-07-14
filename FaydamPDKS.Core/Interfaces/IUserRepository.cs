using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailWithRoleAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRoleAsync(Guid userId, bool asTracking, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllWithRoleAsync(CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default);
    Task<bool> EmployeeNumberExistsAsync(string normalizedEmployeeNumber, Guid? excludingUserId = null, CancellationToken cancellationToken = default);
}

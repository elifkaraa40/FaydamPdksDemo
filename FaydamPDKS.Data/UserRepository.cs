using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public Task<User?> GetByEmailWithRoleAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var email = normalizedEmail.Trim().ToLowerInvariant();
        return Context.Users.AsNoTracking().Include(x => x.Role).Include(x => x.Workplace).Include(x => x.Department)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdWithRoleAsync(Guid userId, bool asTracking, CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = Context.Users.Include(x => x.Role).Include(x => x.Workplace).Include(x => x.Department);
        if (!asTracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllWithRoleAsync(CancellationToken cancellationToken = default) =>
        await Context.Users.AsNoTracking().Include(x => x.Role).Include(x => x.Workplace).Include(x => x.Department)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default)
    {
        var email = normalizedEmail.Trim().ToLowerInvariant();
        return Context.Users.AnyAsync(x => x.Email == email && (!excludingUserId.HasValue || x.Id != excludingUserId.Value), cancellationToken);
    }

    public Task<bool> EmployeeNumberExistsAsync(string normalizedEmployeeNumber, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        Context.Users.AnyAsync(x => x.EmployeeNumber == normalizedEmployeeNumber && (!excludingUserId.HasValue || x.Id != excludingUserId.Value), cancellationToken);
}

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

    public async Task<IReadOnlyList<User>> SearchAsync(string query, int take = 6, CancellationToken cancellationToken = default)
    {
        var term = query.Trim().ToLowerInvariant();
        return await Context.Users.AsNoTracking()
            .Include(x => x.Department)
            .Where(x => x.Name.ToLower().Contains(term)
                || x.Email.ToLower().Contains(term)
                || x.EmployeeNumber.ToLower().Contains(term))
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .Take(Math.Clamp(take, 1, 10))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default)
    {
        var email = normalizedEmail.Trim().ToLowerInvariant();
        return Context.Users.AnyAsync(x => x.Email == email && (!excludingUserId.HasValue || x.Id != excludingUserId.Value), cancellationToken);
    }

    public Task<bool> EmployeeNumberExistsAsync(string normalizedEmployeeNumber, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        Context.Users.AnyAsync(x => x.EmployeeNumber == normalizedEmployeeNumber && (!excludingUserId.HasValue || x.Id != excludingUserId.Value), cancellationToken);

    public Task<bool> PhoneNumberExistsAsync(string normalizedPhoneNumber, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        Context.Users.AnyAsync(x => x.PhoneNumber == normalizedPhoneNumber && (!excludingUserId.HasValue || x.Id != excludingUserId.Value), cancellationToken);

    public async Task<bool> HasRelatedRecordsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Context.AccessLogs.AnyAsync(x => x.UserId == userId, cancellationToken)
        || await Context.LeaveRequests.AnyAsync(x => x.UserId == userId || x.ReviewedByUserId == userId, cancellationToken)
        || await Context.AttendanceCorrectionRequests.AnyAsync(x => x.UserId == userId || x.ReviewedByUserId == userId, cancellationToken)
        || await Context.EmployeeShiftAssignments.AnyAsync(x => x.EmployeeId == userId, cancellationToken)
        || await Context.BreakRecords.AnyAsync(x => x.UserId == userId, cancellationToken)
        || await Context.WorkLocationAssignments.AnyAsync(x => x.UserId == userId || x.CreatedByUserId == userId || x.EndedByUserId == userId, cancellationToken)
        || await Context.FieldWorkRequests.AnyAsync(x => x.UserId == userId || x.ReviewedByUserId == userId, cancellationToken)
        || await Context.RefreshTokens.AnyAsync(x => x.UserId == userId, cancellationToken)
        || await Context.Notifications.AnyAsync(x => x.UserId == userId, cancellationToken)
        || await Context.Permissions.AnyAsync(x => x.UserId == userId, cancellationToken);
}

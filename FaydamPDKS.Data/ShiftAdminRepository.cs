using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class ShiftAdminRepository(AppDbContext context) : IShiftAdminRepository
{
    public async Task<IReadOnlyList<Shift>> GetShiftsAsync(CancellationToken cancellationToken = default) =>
        await context.Shifts.AsNoTracking().OrderBy(x => x.StartsAt).ThenBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EmployeeShiftAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default) =>
        await context.EmployeeShiftAssignments.AsNoTracking().Include(x => x.Employee).Include(x => x.Shift)
            .OrderByDescending(x => x.ValidFrom).ThenBy(x => x.Employee!.Name).ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        context.Shifts.AnyAsync(x => x.Name == name, cancellationToken);

    public Task<bool> ActiveShiftExistsAsync(Guid shiftId, CancellationToken cancellationToken = default) =>
        context.Shifts.AnyAsync(x => x.Id == shiftId && x.IsActive, cancellationToken);

    public Task<bool> HasOverlapAsync(Guid employeeId, DateOnly from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var upper = to ?? DateOnly.MaxValue;
        return context.EmployeeShiftAssignments.AnyAsync(x => x.EmployeeId == employeeId &&
            x.ValidFrom <= upper && (!x.ValidTo.HasValue || x.ValidTo.Value >= from), cancellationToken);
    }

    public async Task AddShiftAsync(Shift shift, CancellationToken cancellationToken = default) => await context.Shifts.AddAsync(shift, cancellationToken);
    public async Task AddAssignmentAsync(EmployeeShiftAssignment assignment, CancellationToken cancellationToken = default) => await context.EmployeeShiftAssignments.AddAsync(assignment, cancellationToken);
}

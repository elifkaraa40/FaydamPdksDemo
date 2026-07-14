using FaydamPDKS.Core.Attendance;
using FaydamPDKS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class ShiftResolver(AppDbContext context) : IShiftResolver
{
    public async Task<ShiftDefinition?> ResolveAsync(Guid employeeId, DateOnly workDate, CancellationToken cancellationToken = default)
    {
        var shift = await context.EmployeeShiftAssignments.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.ValidFrom <= workDate &&
                        (!x.ValidTo.HasValue || x.ValidTo.Value >= workDate) && x.Shift!.IsActive)
            .OrderByDescending(x => x.ValidFrom)
            .Select(x => x.Shift)
            .FirstOrDefaultAsync(cancellationToken);

        return shift is null ? null : new ShiftDefinition(
            shift.StartsAt, shift.EndsAt, shift.LateToleranceMinutes,
            shift.EarlyLeaveToleranceMinutes, shift.BreakMinutes);
    }
}

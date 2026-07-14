using FaydamPDKS.Core.Attendance;

namespace FaydamPDKS.Core.Interfaces;

public interface IShiftResolver
{
    Task<ShiftDefinition?> ResolveAsync(Guid employeeId, DateOnly workDate, CancellationToken cancellationToken = default);
}

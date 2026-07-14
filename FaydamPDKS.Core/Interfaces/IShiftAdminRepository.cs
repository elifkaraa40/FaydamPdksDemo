using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface IShiftAdminRepository
{
    Task<IReadOnlyList<Shift>> GetShiftsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeShiftAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ActiveShiftExistsAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlapAsync(Guid employeeId, DateOnly from, DateOnly? to, CancellationToken cancellationToken = default);
    Task AddShiftAsync(Shift shift, CancellationToken cancellationToken = default);
    Task AddAssignmentAsync(EmployeeShiftAssignment assignment, CancellationToken cancellationToken = default);
}

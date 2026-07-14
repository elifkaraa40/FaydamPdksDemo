using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IShiftAdminService
{
    Task<ShiftAdminPageDto> GetPageAsync(CancellationToken cancellationToken = default);
    Task CreateShiftAsync(CreateShiftDto request, CancellationToken cancellationToken = default);
    Task AssignAsync(CreateShiftAssignmentDto request, CancellationToken cancellationToken = default);
}

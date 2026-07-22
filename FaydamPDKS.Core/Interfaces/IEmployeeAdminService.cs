using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IEmployeeAdminService
{
    Task<EmployeeAdminPageDto> GetPageAsync(CancellationToken cancellationToken = default);
    Task<UpdateEmployeeDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CreateEmployeeDto request, Guid actorId, string? correlationId = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(UpdateEmployeeDto request, Guid actorId, string? correlationId = null, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(Guid id, bool active, Guid actorId, string? correlationId = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid actorId, string? correlationId = null, CancellationToken cancellationToken = default);
}

using FaydamPDKS.Core.Models;
using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IOrganizationRepository
{
    Task<IReadOnlyList<Workplace>> GetWorkplacesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<bool> WorkplaceCodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> DepartmentCodeExistsAsync(Guid workplaceId, string code, CancellationToken cancellationToken = default);
    Task<bool> ActiveWorkplaceExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ActiveDepartmentExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddWorkplaceAsync(Workplace workplace, CancellationToken cancellationToken = default);
    Task AddDepartmentAsync(Department department, CancellationToken cancellationToken = default);
}

public interface IOrganizationAdminService
{
    Task<OrganizationPageDto> GetPageAsync(CancellationToken cancellationToken = default);
    Task CreateWorkplaceAsync(CreateWorkplaceDto request, CancellationToken cancellationToken = default);
    Task CreateDepartmentAsync(CreateDepartmentDto request, CancellationToken cancellationToken = default);
}

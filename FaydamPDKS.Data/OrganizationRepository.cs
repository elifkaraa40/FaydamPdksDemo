using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class OrganizationRepository(AppDbContext context) : IOrganizationRepository
{
    public async Task<IReadOnlyList<Workplace>> GetWorkplacesAsync(CancellationToken cancellationToken = default) =>
        await context.Workplaces.AsNoTracking().Include(x => x.Departments).OrderBy(x => x.Name).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default) =>
        await context.Departments.AsNoTracking().Include(x => x.Workplace).OrderBy(x => x.Workplace.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken);
    public Task<bool> WorkplaceCodeExistsAsync(string code, CancellationToken cancellationToken = default) => context.Workplaces.AnyAsync(x => x.Code == code, cancellationToken);
    public Task<bool> DepartmentCodeExistsAsync(Guid workplaceId, string code, CancellationToken cancellationToken = default) => context.Departments.AnyAsync(x => x.WorkplaceId == workplaceId && x.Code == code, cancellationToken);
    public Task<bool> ActiveWorkplaceExistsAsync(Guid id, CancellationToken cancellationToken = default) => context.Workplaces.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
    public Task<bool> ActiveDepartmentExistsAsync(Guid id, CancellationToken cancellationToken = default) => context.Departments.AnyAsync(x => x.Id == id && x.IsActive && x.Workplace.IsActive, cancellationToken);
    public Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default) => context.Departments.AsNoTracking().Include(x => x.Workplace).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task AddWorkplaceAsync(Workplace workplace, CancellationToken cancellationToken = default) => await context.Workplaces.AddAsync(workplace, cancellationToken);
    public async Task AddDepartmentAsync(Department department, CancellationToken cancellationToken = default) => await context.Departments.AddAsync(department, cancellationToken);
}

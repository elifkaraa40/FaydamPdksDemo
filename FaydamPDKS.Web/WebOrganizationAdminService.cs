using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Web;

public sealed class WebOrganizationAdminService(IOrganizationRepository organizations, IUnitOfWork unitOfWork) : IOrganizationAdminService
{
    public async Task<OrganizationPageDto> GetPageAsync(CancellationToken cancellationToken = default)
    {
        var workplaces = (await organizations.GetWorkplacesAsync(cancellationToken)).Select(x => new WorkplaceListItemDto(x.Id, x.Code, x.Name, x.TimeZoneId, x.Address, x.IsActive, x.Departments.Count)).ToArray();
        var departments = (await organizations.GetDepartmentsAsync(cancellationToken)).Select(x => new DepartmentListItemDto(x.Id, x.WorkplaceId, x.Workplace.Name, x.Code, x.Name, x.IsActive)).ToArray();
        return new(workplaces, departments);
    }

    public async Task CreateWorkplaceAsync(CreateWorkplaceDto request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await organizations.WorkplaceCodeExistsAsync(code, cancellationToken)) throw new InvalidOperationException("Bu işyeri kodu zaten kullanılıyor.");
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId.Trim()); }
        catch (TimeZoneNotFoundException) { throw new InvalidOperationException("Geçerli bir saat dilimi girin."); }
        await organizations.AddWorkplaceAsync(new Workplace { Id = Guid.NewGuid(), Code = code, Name = request.Name.Trim(), TimeZoneId = request.TimeZoneId.Trim(), Address = Clean(request.Address), IsActive = true }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateDepartmentAsync(CreateDepartmentDto request, CancellationToken cancellationToken = default)
    {
        if (!await organizations.ActiveWorkplaceExistsAsync(request.WorkplaceId, cancellationToken)) throw new InvalidOperationException("Aktif işyeri bulunamadı.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await organizations.DepartmentCodeExistsAsync(request.WorkplaceId, code, cancellationToken)) throw new InvalidOperationException("Bu işyerinde bölüm kodu zaten kullanılıyor.");
        await organizations.AddDepartmentAsync(new Department { Id = Guid.NewGuid(), WorkplaceId = request.WorkplaceId, Code = code, Name = request.Name.Trim(), IsActive = true }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

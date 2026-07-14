using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Web;

public sealed class WebEmployeeAdminService(
    IUserRepository users,
    IRoleRepository roles,
    IOrganizationRepository organizations,
    IAuditTrail auditTrail,
    IUnitOfWork unitOfWork) : IEmployeeAdminService
{
    public async Task<EmployeeAdminPageDto> GetPageAsync(CancellationToken cancellationToken = default)
    {
        var employees = (await users.GetAllWithRoleAsync(cancellationToken)).Select(x => new EmployeeListItemDto(
            x.Id, x.EmployeeNumber, x.Name, x.Email, x.Workplace?.Name, x.Department?.Name ?? x.DepartmentLegacy, x.HireDate,
            x.Role?.Name ?? "Tanımsız", x.IsActive)).ToArray();
        var roleOptions = (await roles.GetAllAsync(cancellationToken)).Select(x => new RoleOptionDto(x.Id, x.Name, x.Description)).ToArray();
        var departmentOptions = (await organizations.GetDepartmentsAsync(cancellationToken)).Where(x => x.IsActive && x.Workplace.IsActive)
            .Select(x => new DepartmentOptionDto(x.Id, x.WorkplaceId, x.Workplace.Name, x.Name)).ToArray();
        return new EmployeeAdminPageDto(employees, roleOptions, departmentOptions);
    }

    public async Task<UpdateEmployeeDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdWithRoleAsync(id, false, cancellationToken);
        return user is null ? null : new UpdateEmployeeDto
        {
            Id = user.Id, EmployeeNumber = user.EmployeeNumber, FullName = user.Name,
            Email = user.Email, DepartmentId = user.DepartmentId, HireDate = user.HireDate, RoleId = user.RoleId
        };
    }

    public async Task<Guid> CreateAsync(CreateEmployeeDto request, Guid actorId, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var employeeNumber = request.EmployeeNumber.Trim().ToUpperInvariant();
        await ValidateUniqueAsync(email, employeeNumber, null, cancellationToken);
        if (!await roles.ExistsAsync(request.RoleId, cancellationToken)) throw new InvalidOperationException("Seçilen rol bulunamadı.");
        var department = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(), EmployeeNumber = employeeNumber, Name = request.FullName.Trim(), Email = email,
            DepartmentId = department?.Id, WorkplaceId = department?.WorkplaceId, DepartmentLegacy = department?.Name,
            HireDate = request.HireDate, RoleId = request.RoleId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.TemporaryPassword), IsActive = true
        };
        await users.AddAsync(user, cancellationToken);
        await auditTrail.RecordAsync(actorId, "Employee.Created", nameof(User), user.Id.ToString(), null,
            new { user.EmployeeNumber, user.Name, user.Email, user.WorkplaceId, user.DepartmentId, user.HireDate, user.RoleId, user.IsActive }, correlationId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<bool> UpdateAsync(UpdateEmployeeDto request, Guid actorId, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdWithRoleAsync(request.Id, true, cancellationToken);
        if (user is null) return false;
        var email = request.Email.Trim().ToLowerInvariant();
        var employeeNumber = request.EmployeeNumber.Trim().ToUpperInvariant();
        await ValidateUniqueAsync(email, employeeNumber, user.Id, cancellationToken);
        if (!await roles.ExistsAsync(request.RoleId, cancellationToken)) throw new InvalidOperationException("Seçilen rol bulunamadı.");
        var department = await ResolveDepartmentAsync(request.DepartmentId, cancellationToken);
        var oldValues = new { user.EmployeeNumber, user.Name, user.Email, user.WorkplaceId, user.DepartmentId, user.HireDate, user.RoleId };

        user.EmployeeNumber = employeeNumber;
        user.Name = request.FullName.Trim();
        user.Email = email;
        user.DepartmentId = department?.Id;
        user.WorkplaceId = department?.WorkplaceId;
        user.DepartmentLegacy = department?.Name;
        user.HireDate = request.HireDate;
        user.RoleId = request.RoleId;
        await auditTrail.RecordAsync(actorId, "Employee.Updated", nameof(User), user.Id.ToString(), oldValues,
            new { user.EmployeeNumber, user.Name, user.Email, user.WorkplaceId, user.DepartmentId, user.HireDate, user.RoleId }, correlationId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetActiveAsync(Guid id, bool active, Guid actorId, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        if (id == actorId && !active) throw new InvalidOperationException("Kendi hesabınızı pasife alamazsınız.");
        var user = await users.GetByIdWithRoleAsync(id, true, cancellationToken);
        if (user is null) return false;
        var oldActive = user.IsActive;
        user.IsActive = active;
        await auditTrail.RecordAsync(actorId, active ? "Employee.Activated" : "Employee.Deactivated", nameof(User), user.Id.ToString(),
            new { IsActive = oldActive }, new { user.IsActive }, correlationId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateUniqueAsync(string email, string employeeNumber, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await users.EmailExistsAsync(email, excludingId, cancellationToken))
            throw new InvalidOperationException("Bu e-posta adresi başka bir personelde kullanılıyor.");
        if (await users.EmployeeNumberExistsAsync(employeeNumber, excludingId, cancellationToken))
            throw new InvalidOperationException("Bu sicil numarası başka bir personelde kullanılıyor.");
    }

    private async Task<FaydamPDKS.Core.Models.Department?> ResolveDepartmentAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return null;
        var department = await organizations.GetDepartmentAsync(id.Value, cancellationToken);
        if (department is null || !department.IsActive || !department.Workplace.IsActive)
            throw new InvalidOperationException("Aktif bölüm bulunamadı.");
        return department;
    }
}

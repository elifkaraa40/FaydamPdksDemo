using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record EmployeeListItemDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string Email,
    string? Workplace,
    string? Department,
    DateOnly? HireDate,
    string Role,
    bool IsActive);

public sealed class CreateEmployeeDto
{
    [Required, StringLength(40)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    public DateOnly? HireDate { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    [Required, MinLength(12), DataType(DataType.Password)]
    public string TemporaryPassword { get; set; } = string.Empty;
}

public sealed class UpdateEmployeeDto
{
    public Guid Id { get; set; }

    [Required, StringLength(40)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    public DateOnly? HireDate { get; set; }

    [Required]
    public Guid RoleId { get; set; }
}

public sealed record RoleOptionDto(Guid Id, string Name, string? Description);
public sealed record DepartmentOptionDto(Guid Id, Guid WorkplaceId, string WorkplaceName, string DepartmentName);
public sealed record EmployeeAdminPageDto(IReadOnlyList<EmployeeListItemDto> Employees, IReadOnlyList<RoleOptionDto> Roles, IReadOnlyList<DepartmentOptionDto> Departments);

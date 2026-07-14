using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record WorkplaceListItemDto(Guid Id, string Code, string Name, string TimeZoneId, string? Address, bool IsActive, int DepartmentCount);
public sealed record DepartmentListItemDto(Guid Id, Guid WorkplaceId, string WorkplaceName, string Code, string Name, bool IsActive);
public sealed record OrganizationPageDto(IReadOnlyList<WorkplaceListItemDto> Workplaces, IReadOnlyList<DepartmentListItemDto> Departments);

public sealed class CreateWorkplaceDto
{
    [Required, StringLength(30)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(100)] public string TimeZoneId { get; set; } = "Europe/Istanbul";
    [StringLength(250)] public string? Address { get; set; }
}

public sealed class CreateDepartmentDto
{
    [Required] public Guid WorkplaceId { get; set; }
    [Required, StringLength(30)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
}

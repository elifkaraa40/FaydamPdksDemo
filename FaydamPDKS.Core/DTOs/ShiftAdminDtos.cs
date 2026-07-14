using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record ShiftListItemDto(Guid Id, string Name, TimeOnly StartsAt, TimeOnly EndsAt, int BreakMinutes, int LateToleranceMinutes, int EarlyLeaveToleranceMinutes, bool IsActive);
public sealed record ShiftAssignmentListItemDto(Guid Id, Guid EmployeeId, string EmployeeName, string EmployeeNumber, string ShiftName, DateOnly ValidFrom, DateOnly? ValidTo);
public sealed record EmployeeOptionDto(Guid Id, string EmployeeNumber, string FullName);
public sealed record ShiftAdminPageDto(IReadOnlyList<ShiftListItemDto> Shifts, IReadOnlyList<ShiftAssignmentListItemDto> Assignments, IReadOnlyList<EmployeeOptionDto> Employees);

public sealed class CreateShiftDto
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required] public TimeOnly StartsAt { get; set; }
    [Required] public TimeOnly EndsAt { get; set; }
    [Range(0, 240)] public int LateToleranceMinutes { get; set; } = 5;
    [Range(0, 240)] public int EarlyLeaveToleranceMinutes { get; set; } = 5;
    [Range(0, 720)] public int BreakMinutes { get; set; } = 60;
}

public sealed class CreateShiftAssignmentDto
{
    [Required] public Guid EmployeeId { get; set; }
    [Required] public Guid ShiftId { get; set; }
    [Required] public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

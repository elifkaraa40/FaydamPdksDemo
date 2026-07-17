using System.ComponentModel.DataAnnotations;
using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.DTOs;

public sealed class CreateWorkLocationAssignmentDto
{
    [Required] public Guid UserId { get; set; }
    public WorkLocationType LocationType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public WorkLocationRecurrenceType RecurrenceType { get; set; }
    public DayOfWeek[] Days { get; set; } = [];
    [StringLength(500)] public string? Reason { get; set; }
    [StringLength(150)] public string? ProjectName { get; set; }
    [StringLength(150)] public string? CustomerName { get; set; }
    [StringLength(300)] public string? FieldAddress { get; set; }
}

public sealed class CreateFieldWorkRequestDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public WorkLocationRecurrenceType RecurrenceType { get; set; } = WorkLocationRecurrenceType.EveryWorkday;
    public DayOfWeek[] Days { get; set; } = [];
    [Required, StringLength(150)] public string ProjectName { get; set; } = string.Empty;
    [StringLength(150)] public string? CustomerName { get; set; }
    [StringLength(300)] public string? FieldAddress { get; set; }
    [Required, StringLength(500, MinimumLength = 10)] public string Reason { get; set; } = string.Empty;
}

public sealed record WorkLocationAssignmentDto(Guid Id, Guid UserId, string EmployeeName, WorkLocationType LocationType,
    DateOnly StartDate, DateOnly? EndDate, WorkLocationRecurrenceType RecurrenceType, DayOfWeek[] Days,
    string? Reason, string? ProjectName, string? CustomerName, string? FieldAddress, bool IsActive);

public sealed record FieldWorkRequestDto(Guid Id, Guid UserId, string EmployeeName, DateOnly StartDate, DateOnly EndDate,
    WorkLocationRecurrenceType RecurrenceType, DayOfWeek[] Days, string ProjectName, string? CustomerName,
    string? FieldAddress, string Reason, WorkLocationRequestStatus Status, DateTimeOffset CreatedAt, string? ReviewNote);

public sealed record WorkLocationPageDto(IReadOnlyList<WorkLocationAssignmentDto> Assignments,
    IReadOnlyList<FieldWorkRequestDto> Requests, IReadOnlyList<EmployeeOptionDto> Employees, bool FeatureEnabled);

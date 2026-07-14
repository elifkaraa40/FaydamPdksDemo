namespace FaydamPDKS.Core.DTOs;

public sealed record DashboardDto(
    DateOnly WorkDate,
    int TotalPersonnel,
    int PresentCount,
    int LateCount,
    int OnLeaveCount,
    int MissingRecordCount,
    int PendingLeaveCount,
    IReadOnlyList<DashboardMovementDto> RecentMovements,
    IReadOnlyList<DashboardLeaveDto> PendingLeaves);

public sealed record DashboardMovementDto(
    string EmployeeName,
    string EmployeeCode,
    DateTimeOffset OccurredAt,
    string EventType,
    string ZoneName);

public sealed record DashboardLeaveDto(
    Guid Id,
    string EmployeeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string LeaveType);

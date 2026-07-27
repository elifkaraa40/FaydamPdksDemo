namespace FaydamPDKS.Core.DTOs;

public sealed record DashboardDto(
    DateOnly WorkDate,
    int TotalPersonnel,
    int PresentCount,
    int LateCount,
    int OnLeaveCount,
    int MissingRecordCount,
    int PendingLeaveCount,
    IReadOnlyList<DashboardDailyAttendanceDto> DailyAttendance,
    IReadOnlyList<DashboardStatusPersonnelDto> PresentPersonnel,
    IReadOnlyList<DashboardStatusPersonnelDto> LatePersonnel,
    IReadOnlyList<DashboardStatusPersonnelDto> OnLeavePersonnel,
    IReadOnlyList<DashboardStatusPersonnelDto> MissingRecordPersonnel,
    IReadOnlyList<DashboardMovementDto> RecentMovements,
    IReadOnlyList<DashboardLeaveDto> PendingLeaves);

public sealed record DashboardDailyAttendanceDto(
    DateOnly WorkDate,
    int TotalPersonnel,
    IReadOnlyList<DashboardStatusPersonnelDto> OnTimePersonnel,
    IReadOnlyList<DashboardStatusPersonnelDto> LatePersonnel,
    IReadOnlyList<DashboardStatusPersonnelDto> OnLeavePersonnel,
    IReadOnlyList<DashboardStatusPersonnelDto> MissingRecordPersonnel);

public sealed record DashboardStatusPersonnelDto(
    Guid Id,
    string Name,
    string EmployeeNumber);

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

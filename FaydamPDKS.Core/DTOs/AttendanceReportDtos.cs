namespace FaydamPDKS.Core.DTOs;

public sealed record AttendanceReportRowDto(
    Guid EmployeeId, string EmployeeNumber, string EmployeeName, string? Department,
    DateOnly WorkDate, string ShiftName, string Status, DateTimeOffset? FirstEntry,
    DateTimeOffset? LastExit, int WorkedMinutes, int ExpectedMinutes, int LateMinutes, int OvertimeMinutes);

public sealed record AttendanceReportDto(DateOnly From, DateOnly To, IReadOnlyList<AttendanceReportRowDto> Rows);

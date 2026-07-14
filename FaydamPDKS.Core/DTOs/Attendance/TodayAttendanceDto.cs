namespace FaydamPDKS.Core.DTOs.Attendance;

public sealed record TodayAttendanceDto(
    DateOnly WorkDate,
    string Status,
    DateTimeOffset? FirstEntry,
    DateTimeOffset? LastExit,
    int WorkedMinutes,
    int ExpectedMinutes,
    int LateMinutes,
    int OvertimeMinutes);

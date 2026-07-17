namespace FaydamPDKS.Core.Attendance;

public enum AttendanceStatus
{
    Complete,
    MissingEntry,
    MissingExit,
    NoRecord,
    NonWorkingDay,
    RemoteWork,
    FieldWork
}

public sealed record DailyAttendanceResult(
    DateOnly WorkDate,
    AttendanceStatus Status,
    DateTimeOffset? FirstEntry,
    DateTimeOffset? LastExit,
    int WorkedMinutes,
    int ExpectedMinutes,
    int LateMinutes,
    int EarlyLeaveMinutes,
    int OvertimeMinutes)
{
    public int MissingMinutes => Math.Max(0, ExpectedMinutes - WorkedMinutes);
}

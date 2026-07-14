namespace FaydamPDKS.Core.Attendance;

public enum AttendanceEventType
{
    Entry = 1,
    Exit = 2
}

public sealed record AttendanceEvent(
    Guid EmployeeId,
    DateTimeOffset OccurredAt,
    AttendanceEventType Type,
    string SourceId);

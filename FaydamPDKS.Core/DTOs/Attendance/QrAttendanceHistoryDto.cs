namespace FaydamPDKS.Core.DTOs.Attendance;

public sealed record QrAttendanceHistoryDto(
    int Id,
    DateTimeOffset OccurredAt,
    string EventType);

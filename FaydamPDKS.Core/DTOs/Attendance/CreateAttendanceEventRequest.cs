using System.ComponentModel.DataAnnotations;
using FaydamPDKS.Core.Attendance;

namespace FaydamPDKS.Core.DTOs.Attendance;

public sealed record CreateAttendanceEventRequest(
    [EnumDataType(typeof(AttendanceEventType))] AttendanceEventType EventType,
    DateTimeOffset OccurredAt,
    [Required, MaxLength(100)] string DeviceEventId,
    [Range(1, int.MaxValue)] int ZoneId);

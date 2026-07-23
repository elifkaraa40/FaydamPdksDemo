using FaydamPDKS.Core.Attendance;
using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record AttendanceQrListItemDto(Guid Id, string Name, string WorkplaceName, string ZoneName,
    AttendanceEventType EventType, bool IsActive, bool IsLegacy, DateTimeOffset CreatedAt, DateTimeOffset? RevokedAt);

public sealed record AttendanceTransitionDto(string EmployeeName, string EmployeeNumber, string ZoneName,
    string EventType, DateTimeOffset OccurredAt, string Source);

public sealed record AttendanceQrPageDto(IReadOnlyList<AttendanceQrListItemDto> QrCodes,
    IReadOnlyList<AttendanceTransitionDto> RecentTransitions, IReadOnlyList<WorkplaceOptionDto> Workplaces,
    IReadOnlyList<ZoneOptionDto> Zones);

public sealed record ZoneOptionDto(int Id, string Name);

public sealed class CreateAttendanceQrDto
{
    [Required] public Guid WorkplaceId { get; set; }
    [Range(1, int.MaxValue)] public int ZoneId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [EnumDataType(typeof(AttendanceEventType))] public AttendanceEventType EventType { get; set; }
}

public sealed record GeneratedAttendanceQrDto(Guid Id, string Name, string RawValue, AttendanceEventType EventType);

public sealed record ScanAttendanceQrRequest([Required] string QrValue, DateTimeOffset OccurredAt,
    [Required, MaxLength(100)] string DeviceEventId,
    [Required, StringLength(200, MinimumLength = 16)] string DeviceId);

public sealed record ScanAttendanceQrResponse(string EventType, string WorkplaceName, string ZoneName,
    DateTimeOffset OccurredAt);

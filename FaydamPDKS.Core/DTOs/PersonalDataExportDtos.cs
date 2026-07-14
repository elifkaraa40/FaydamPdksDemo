namespace FaydamPDKS.Core.DTOs;

public sealed record PersonalDataProfileDto(Guid Id, string EmployeeNumber, string FullName, string Email, string? PhoneNumber, string? Workplace, string? Department, DateOnly? HireDate, bool IsActive, bool IsEmailNotificationEnabled, bool IsSmsNotificationEnabled);
public sealed record PersonalAttendanceEventDto(int Id, DateTimeOffset OccurredAt, string EventType, int ZoneId, string Source, string? DeviceEventId);
public sealed record PersonalLeaveDto(Guid Id, string LeaveType, DateOnly StartDate, DateOnly EndDate, string? Reason, string Status, DateTimeOffset CreatedAt, DateTimeOffset? ReviewedAt, string? ReviewNote);
public sealed record PersonalCorrectionDto(Guid Id, DateOnly WorkDate, TimeOnly RequestedEntry, TimeOnly RequestedExit, string Reason, string Status, DateTimeOffset CreatedAt, DateTimeOffset? ReviewedAt, string? ReviewNote);
public sealed record PersonalNotificationDto(Guid Id, string Type, string Title, string Message, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);
public sealed record PersonalDataExportDto(DateTimeOffset GeneratedAt, PersonalDataProfileDto Profile, IReadOnlyList<PersonalAttendanceEventDto> AttendanceEvents, IReadOnlyList<PersonalLeaveDto> LeaveRequests, IReadOnlyList<PersonalCorrectionDto> AttendanceCorrections, IReadOnlyList<PersonalNotificationDto> Notifications);

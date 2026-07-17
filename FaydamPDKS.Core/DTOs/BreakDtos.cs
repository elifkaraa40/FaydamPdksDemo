using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record CurrentBreakDto(bool IsOnBreak, Guid? BreakId, DateTimeOffset? StartedAt);
public sealed record BreakHistoryItemDto(Guid Id, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, int? DurationMinutes, bool AutoClosed);
public sealed record ActiveColleagueBreakDto(Guid UserId, string FullName, string? Department, DateTimeOffset StartedAt);
public sealed record StartBreakRequest([Required, MaxLength(100)] string DeviceEventId);
public sealed record EndBreakRequest([Required, MaxLength(100)] string DeviceEventId);

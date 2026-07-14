using FaydamPDKS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record CreateLeaveRequestDto(
    [EnumDataType(typeof(LeaveType))] LeaveType LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    [StringLength(500)] string? Reason);

public sealed record LeaveRequestDto(
    Guid Id,
    LeaveType LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int CalendarDayCount,
    string? Reason,
    LeaveRequestStatus Status,
    DateTimeOffset CreatedAt,
    string? ReviewNote);

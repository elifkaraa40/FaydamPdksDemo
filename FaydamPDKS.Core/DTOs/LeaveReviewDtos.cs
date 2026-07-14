using FaydamPDKS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record LeaveReviewListItemDto(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    LeaveType LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int CalendarDayCount,
    string? Reason,
    LeaveRequestStatus Status,
    DateTimeOffset CreatedAt,
    string? ReviewNote);

public sealed record ReviewLeaveRequestDto(
    bool Approve,
    [StringLength(500)] string? Note);

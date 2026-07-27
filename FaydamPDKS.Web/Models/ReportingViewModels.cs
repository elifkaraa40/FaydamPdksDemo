using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Web.Models;

public sealed record TransitionReportRow(
    int Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    string? Department,
    DateOnly WorkDate,
    TimeOnly EventTime,
    string EventType,
    string ZoneName,
    string? WorkplaceName,
    string Source);

public sealed record TransitionReportViewModel(
    DateOnly From,
    DateOnly To,
    Guid? SelectedEmployeeId,
    string? SelectedEventType,
    IReadOnlyList<TransitionReportRow> Rows,
    IReadOnlyList<EmployeeOptionDto> Employees);

public sealed record LeaveReportRow(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    string? Department,
    LeaveType LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    LeaveDayPortion DayPortion,
    double WorkDayCount,
    LeaveRequestStatus Status,
    string? Reason);

public sealed record LeaveReportViewModel(
    DateOnly From,
    DateOnly To,
    Guid? SelectedEmployeeId,
    LeaveType? SelectedLeaveType,
    LeaveRequestStatus? SelectedStatus,
    IReadOnlyList<LeaveReportRow> Rows,
    IReadOnlyList<EmployeeOptionDto> Employees);

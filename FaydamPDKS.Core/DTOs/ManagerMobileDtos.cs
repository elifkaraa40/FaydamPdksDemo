using System.ComponentModel.DataAnnotations;
using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.DTOs;

public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record ManagerApprovalsSummaryDto(int Registrations, int LeaveRequests, int AttendanceCorrections, int WorkLocationRequests);

public sealed record ManagerDashboardDto(
    ManagerApprovalsSummaryDto PendingApprovals, int EnteredToday, int ExitedToday, int MissingAttendance,
    int OfficePersonnel, int FieldPersonnel, int RemotePersonnel, int PersonnelOnBreak);

public sealed record ManagerRegistrationDto(
    Guid Id, string FullName, string Email, string? PhoneNumber, AccountStatus Status,
    string? EmployeeNumber, Guid? DepartmentId);

public sealed class ReviewRegistrationDto
{
    public bool Approve { get; set; }
    [StringLength(500)] public string? Note { get; set; }
    [StringLength(40)] public string? EmployeeNumber { get; set; }
    public Guid? DepartmentId { get; set; }
}

public sealed record ReviewDecisionDto(bool Approve, [StringLength(500)] string? Note);

public sealed record ManagerPersonnelStatusDto(
    Guid UserId, string EmployeeNumber, string FullName, string? Department, string? Workplace,
    string AttendanceStatus, DateTimeOffset? FirstEntry, DateTimeOffset? LastExit, string WorkLocation,
    bool IsOnBreak, DateTimeOffset? BreakStartedAt, bool MissingRecord);

public sealed record ManagerAttendanceReportDto(DateOnly From, DateOnly To, PagedResultDto<AttendanceReportRowDto> Results);

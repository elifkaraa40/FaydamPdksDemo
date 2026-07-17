using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Web.Models;

public sealed record MyWorkViewModel(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<AttendanceReportRowDto> Attendance,
    IReadOnlyList<BreakHistoryItemDto> Breaks,
    IReadOnlyList<LeaveRequest> LeaveRequests,
    IReadOnlyList<AttendanceCorrectionRequest>? Corrections = null)
{
    public IReadOnlyList<AttendanceCorrectionRequest> CorrectionRequests => Corrections ?? [];
    public IReadOnlyDictionary<Guid, double> LeaveWorkDayCounts { get; init; } = new Dictionary<Guid, double>();
    public int WorkedMinutes => Attendance.Sum(x => x.WorkedMinutes);
    public int ExpectedMinutes => Attendance.Sum(x => x.ExpectedMinutes);
    public int OvertimeMinutes => Attendance.Sum(x => x.OvertimeMinutes);
    public int MissingRecordCount => Attendance.Count(x => x.Status is "MissingEntry" or "MissingExit" or "NoRecord");
    public int CompleteCount => Attendance.Count(x => x.Status == "Complete");
    public int NonWorkingCount => Attendance.Count(x => x.Status == "NonWorkingDay");
    public int PendingLeaveCount => LeaveRequests.Count(x => x.Status == FaydamPDKS.Core.Enums.LeaveRequestStatus.Pending);
    public AttendanceReportRowDto? Today => Attendance.FirstOrDefault(x => x.WorkDate == DateOnly.FromDateTime(DateTime.Today));
}

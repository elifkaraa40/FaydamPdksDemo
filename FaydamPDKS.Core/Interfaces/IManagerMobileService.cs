using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.Interfaces;

public interface IManagerMobileService
{
    Task<ManagerDashboardDto> GetDashboardAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task<ManagerApprovalsSummaryDto> GetApprovalsSummaryAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ManagerRegistrationDto>> GetRegistrationsAsync(Guid managerId, AccountStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ReviewRegistrationAsync(Guid id, Guid managerId, ReviewRegistrationDto request, string? correlationId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<LeaveReviewListItemDto>> GetLeaveRequestsAsync(Guid managerId, LeaveRequestStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ReviewLeaveRequestAsync(Guid id, Guid managerId, ReviewLeaveRequestDto request, string? correlationId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<AttendanceCorrectionReviewDto>> GetAttendanceCorrectionsAsync(Guid managerId, AttendanceCorrectionStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ReviewAttendanceCorrectionAsync(Guid id, Guid managerId, ReviewAttendanceCorrectionDto request, string? correlationId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<FieldWorkRequestDto>> GetWorkLocationRequestsAsync(Guid managerId, WorkLocationRequestStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ReviewWorkLocationRequestAsync(Guid id, Guid managerId, bool approve, string? note, string? correlationId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ManagerPersonnelStatusDto>> GetPersonnelStatusAsync(Guid managerId, Guid? workplaceId, Guid? departmentId, string? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ManagerAttendanceReportDto> GetAttendanceReportAsync(Guid managerId, DateOnly from, DateOnly to, Guid? workplaceId, Guid? departmentId, Guid? userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AttendanceReportDto> GetAttendanceReportExportAsync(Guid managerId, DateOnly from, DateOnly to, Guid? workplaceId, Guid? departmentId, Guid? userId, string? correlationId, CancellationToken cancellationToken = default);
}

using FaydamPDKS.Core.DTOs.Attendance;

namespace FaydamPDKS.Core.Interfaces;

public interface IAttendanceService
{
    Task<TodayAttendanceDto> GetTodayAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TodayAttendanceDto>> GetRangeAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QrAttendanceHistoryDto>> GetQrHistoryAsync(
        Guid employeeId,
        int limit,
        CancellationToken cancellationToken = default);
    Task<bool> AddEventAsync(Guid employeeId, CreateAttendanceEventRequest request, CancellationToken cancellationToken = default);
}

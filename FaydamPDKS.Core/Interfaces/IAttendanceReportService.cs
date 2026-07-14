using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IAttendanceReportService
{
    Task<AttendanceReportDto> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

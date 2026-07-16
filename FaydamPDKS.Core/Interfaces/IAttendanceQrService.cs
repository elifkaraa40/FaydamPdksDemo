using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IAttendanceQrService
{
    Task<AttendanceQrPageDto> GetPageAsync(CancellationToken cancellationToken = default);
    Task<GeneratedAttendanceQrDto> CreateAsync(CreateAttendanceQrDto request, CancellationToken cancellationToken = default);
    Task<GeneratedAttendanceQrDto?> RotateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ScanAttendanceQrResponse?> ScanAsync(Guid employeeId, ScanAttendanceQrRequest request, CancellationToken cancellationToken = default);
}

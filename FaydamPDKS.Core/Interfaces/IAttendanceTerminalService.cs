using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IAttendanceTerminalService
{
    Task<TerminalPageDto> GetPageAsync(CancellationToken cancellationToken = default);
    Task<RegisteredTerminalDto> RegisterAsync(RegisterTerminalDto request, CancellationToken cancellationToken = default);
    Task<RegisteredTerminalDto?> RotateKeyAsync(Guid terminalId, CancellationToken cancellationToken = default);
    Task<bool> HeartbeatAsync(Guid terminalId, string apiKey, TerminalHeartbeatDto request, CancellationToken cancellationToken = default);
}

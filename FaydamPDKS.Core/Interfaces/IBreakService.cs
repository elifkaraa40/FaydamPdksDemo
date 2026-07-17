using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IBreakService
{
    Task<CurrentBreakDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CurrentBreakDto> StartAsync(Guid userId, string deviceEventId, CancellationToken cancellationToken = default);
    Task<CurrentBreakDto> EndAsync(Guid userId, Guid breakId, string deviceEventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BreakHistoryItemDto>> GetHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActiveColleagueBreakDto>> GetActiveColleaguesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int?> GetCompletedMinutesAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}

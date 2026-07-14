using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface IWorkCalendarRepository
{
    Task<IReadOnlyList<WorkCalendarDay>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid? workplaceId, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(WorkCalendarDay day, CancellationToken cancellationToken = default);
}

public interface IWorkCalendarAdminService
{
    Task<WorkCalendarPageDto> GetPageAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(CreateWorkCalendarDayDto request, CancellationToken cancellationToken = default);
}

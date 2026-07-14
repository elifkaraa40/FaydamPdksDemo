using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class WorkCalendarRepository(AppDbContext context) : IWorkCalendarRepository
{
    public async Task<IReadOnlyList<WorkCalendarDay>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.WorkCalendarDays.AsNoTracking().Include(x => x.Workplace).OrderBy(x => x.Date).ThenBy(x => x.WorkplaceId).ToListAsync(cancellationToken);
    public Task<bool> ExistsAsync(Guid? workplaceId, DateOnly date, CancellationToken cancellationToken = default) =>
        context.WorkCalendarDays.AnyAsync(x => x.WorkplaceId == workplaceId && x.Date == date, cancellationToken);
    public async Task AddAsync(WorkCalendarDay day, CancellationToken cancellationToken = default) => await context.WorkCalendarDays.AddAsync(day, cancellationToken);
}

using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public sealed class WorkCalendarResolver(AppDbContext context) : IWorkCalendarResolver
{
    public async Task<WorkdayResolution> ResolveAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var workplaceId = await context.Users.AsNoTracking().Where(x => x.Id == employeeId)
            .Select(x => x.WorkplaceId).SingleOrDefaultAsync(cancellationToken);
        var specialDay = await context.WorkCalendarDays.AsNoTracking()
            .Where(x => x.Date == date && (x.WorkplaceId == workplaceId || x.WorkplaceId == null))
            .OrderByDescending(x => x.WorkplaceId.HasValue).FirstOrDefaultAsync(cancellationToken);
        if (specialDay is not null)
            return new(specialDay.DayType == CalendarDayType.WorkingDayOverride, specialDay.Name);
        var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        return new(!weekend, weekend ? "Hafta tatili" : null);
    }
}

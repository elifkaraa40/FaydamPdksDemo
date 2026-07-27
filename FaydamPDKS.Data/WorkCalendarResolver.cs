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
        {
            if (specialDay.DayType == CalendarDayType.WorkingDayOverride)
                return new(true, specialDay.Name, 1);
            if (specialDay.IsHalfDay)
                return new(true, specialDay.Name, .5, new TimeOnly(13, 0));
            return new(false, specialDay.Name, 0);
        }
        var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        return new(!weekend, weekend ? "Hafta tatili" : null, weekend ? 0 : 1);
    }
}

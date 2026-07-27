using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace FaydamPDKS.Data;

public sealed class PublicHolidaySyncService(
    AppDbContext context,
    IPublicHolidayProvider provider,
    TimeProvider timeProvider) : IPublicHolidaySyncService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> YearLocks = new();

    public async Task<PublicHolidaySyncResult> SyncYearAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var yearLock = YearLocks.GetOrAdd(year, _ => new SemaphoreSlim(1, 1));
        await yearLock.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            var calendar = await provider.GetTurkeyCalendarAsync(year, cancellationToken);
            var start = new DateOnly(year, 1, 1);
            var end = new DateOnly(year, 12, 31);
            var existing = await context.WorkCalendarDays
                .Where(x => x.WorkplaceId == null && x.Date >= start && x.Date <= end)
                .ToListAsync(cancellationToken);

            var added = 0;
            var updated = 0;
            foreach (var holiday in calendar.Holidays)
            {
                var day = existing.SingleOrDefault(x => x.Date == holiday.Date);
                if (day is not null && !day.IsSystemGenerated)
                    continue;

                var changed = false;
                if (day is null)
                {
                    day = new WorkCalendarDay
                    {
                        Id = Guid.NewGuid(),
                        Date = holiday.Date,
                        WorkplaceId = null,
                        IsSystemGenerated = true
                    };
                    context.WorkCalendarDays.Add(day);
                    existing.Add(day);
                    added++;
                    changed = true;
                }
                else if (day.Name != holiday.Name
                         || day.DayType != CalendarDayType.Holiday
                         || day.IsHalfDay != holiday.IsHalfDay
                         || day.Source != holiday.Source)
                {
                    updated++;
                    changed = true;
                }

                day.Name = holiday.Name;
                day.DayType = CalendarDayType.Holiday;
                day.IsHalfDay = holiday.IsHalfDay;
                day.IsSystemGenerated = true;
                day.Source = holiday.Source;
                if (changed)
                    day.SourceUpdatedAt = now;
            }

            if (calendar.IsComplete)
            {
                var officialDates = calendar.Holidays.Select(x => x.Date).ToHashSet();
                var staleRows = existing
                    .Where(x => x.IsSystemGenerated && !officialDates.Contains(x.Date))
                    .ToArray();
                if (staleRows.Length > 0)
                    context.WorkCalendarDays.RemoveRange(staleRows);
            }

            var state = await context.HolidayCalendarSyncStates
                .SingleOrDefaultAsync(x => x.Year == year, cancellationToken);
            if (state is null)
            {
                state = new HolidayCalendarSyncState { Year = year };
                context.HolidayCalendarSyncStates.Add(state);
            }

            state.LastAttemptedAt = now;
            state.SourceUrl = DiyanetPublicHolidayProvider.SourceUrl;
            state.Warning = calendar.Warning;
            if (calendar.IsComplete)
                state.LastSuccessfulAt = now;

            await context.SaveChangesAsync(cancellationToken);
            return new(year, added, updated, calendar.IsComplete, state.LastSuccessfulAt, state.Warning);
        }
        finally
        {
            yearLock.Release();
        }
    }
}

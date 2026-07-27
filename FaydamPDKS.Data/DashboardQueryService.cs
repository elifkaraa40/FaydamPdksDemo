using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FaydamPDKS.Data;

public sealed class DashboardQueryService(
    AppDbContext context,
    IConfiguration configuration,
    TimeProvider timeProvider) : IDashboardQueryService
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var timeZone = ResolveTimeZone(configuration["Attendance:TimeZone"] ?? "Europe/Istanbul");
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var localStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1), timeZone);
        var shiftStart = TimeOnly.Parse(configuration["Attendance:DefaultShiftStart"] ?? "09:00");
        var tolerance = int.TryParse(configuration["Attendance:LateToleranceMinutes"], out var configuredTolerance)
            ? configuredTolerance
            : 5;
        var lateBoundaryUtc = TimeZoneInfo.ConvertTimeToUtc(today.ToDateTime(shiftStart, DateTimeKind.Unspecified).AddMinutes(tolerance), timeZone);

        var personnel = await context.Users.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DashboardStatusPersonnelDto(x.Id, x.Name, x.EmployeeNumber))
            .ToListAsync(cancellationToken);
        var totalPersonnel = personnel.Count;
        var presentUserIds = await context.AccessLogs.AsNoTracking()
            .Where(x => x.LogDate >= fromUtc && x.LogDate < toUtc && x.LogType == "Giris")
            .Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
        var lateUserIds = await context.AccessLogs.AsNoTracking()
            .Where(x => x.LogDate >= fromUtc && x.LogDate < toUtc && x.LogType == "Giris")
            .GroupBy(x => x.UserId)
            .Where(x => x.Min(y => y.LogDate) > lateBoundaryUtc)
            .Select(x => x.Key)
            .ToListAsync(cancellationToken);
        var onLeaveUserIds = await context.LeaveRequests.AsNoTracking()
            .Where(x => x.Status == LeaveRequestStatus.Approved && x.StartDate <= today && x.EndDate >= today)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var pendingLeaveCount = await context.LeaveRequests.AsNoTracking()
            .CountAsync(x => x.Status == LeaveRequestStatus.Pending, cancellationToken);
        var todayWorkplaces = await context.Users.AsNoTracking()
            .Select(x => new { x.Id, x.WorkplaceId })
            .ToDictionaryAsync(x => x.Id, x => x.WorkplaceId, cancellationToken);
        var todayCalendarDays = await context.WorkCalendarDays.AsNoTracking()
            .Where(x => x.Date == today)
            .ToListAsync(cancellationToken);

        var presentSet = presentUserIds.ToHashSet();
        var lateSet = lateUserIds.ToHashSet();
        var onLeaveSet = onLeaveUserIds.ToHashSet();
        var presentPersonnel = personnel.Where(x => presentSet.Contains(x.Id)).ToArray();
        var latePersonnel = personnel.Where(x => lateSet.Contains(x.Id)).ToArray();
        var onLeavePersonnel = personnel.Where(x => onLeaveSet.Contains(x.Id)).ToArray();
        var missingRecordPersonnel = personnel
            .Where(x =>
            {
                var workplaceId = todayWorkplaces.GetValueOrDefault(x.Id);
                var specialDay = todayCalendarDays
                    .Where(day => day.WorkplaceId == null || day.WorkplaceId == workplaceId)
                    .OrderByDescending(day => day.WorkplaceId.HasValue)
                    .FirstOrDefault();
                var isWorkingDay = specialDay is not null
                    ? specialDay.DayType == CalendarDayType.WorkingDayOverride || specialDay.IsHalfDay
                    : today.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
                return isWorkingDay && !presentSet.Contains(x.Id) && !onLeaveSet.Contains(x.Id);
            })
            .ToArray();

        var historyWindowStart = today.AddDays(-21);
        var workplaceByPersonnel = await context.Users.AsNoTracking()
            .Select(x => new { x.Id, x.WorkplaceId })
            .ToDictionaryAsync(x => x.Id, x => x.WorkplaceId, cancellationToken);
        var calendarDays = await context.WorkCalendarDays.AsNoTracking()
            .Where(x => x.Date >= historyWindowStart && x.Date < today)
            .ToListAsync(cancellationToken);

        bool IsWorkingDay(DashboardStatusPersonnelDto employee, DateOnly date)
        {
            var workplaceId = workplaceByPersonnel.GetValueOrDefault(employee.Id);
            var specialDay = calendarDays
                .Where(x => x.Date == date && (x.WorkplaceId == workplaceId || x.WorkplaceId == null))
                .OrderByDescending(x => x.WorkplaceId.HasValue)
                .FirstOrDefault();
            return specialDay is not null
                ? specialDay.DayType == CalendarDayType.WorkingDayOverride || specialDay.IsHalfDay
                : date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
        }

        var workingHistoryDates = Enumerable.Range(0, 21)
            .Select(offset => historyWindowStart.AddDays(offset))
            .Where(date => personnel.Any(employee => IsWorkingDay(employee, date)))
            .TakeLast(7)
            .ToArray();
        var historyDates = workingHistoryDates.Length == 7
            ? workingHistoryDates
            : Enumerable.Range(1, 7).Select(daysAgo => today.AddDays(-daysAgo)).Reverse().ToArray();
        var historyStart = historyDates[0];
        var historyFromUtc = TimeZoneInfo.ConvertTimeToUtc(
            historyStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);
        var historyToUtc = TimeZoneInfo.ConvertTimeToUtc(
            today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);
        var historicalEntries = await context.AccessLogs.AsNoTracking()
            .Where(x => x.LogDate >= historyFromUtc && x.LogDate < historyToUtc && x.LogType == "Giris")
            .Select(x => new { x.UserId, x.LogDate })
            .ToListAsync(cancellationToken);
        var firstEntriesByDate = historicalEntries
            .GroupBy(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc), timeZone)))
            .ToDictionary(
                x => x.Key,
                x => x.GroupBy(y => y.UserId).ToDictionary(
                    y => y.Key,
                    y => y.Min(z => DateTime.SpecifyKind(z.LogDate, DateTimeKind.Utc))));
        var historicalLeaves = await context.LeaveRequests.AsNoTracking()
            .Where(x => x.Status == LeaveRequestStatus.Approved &&
                        x.StartDate <= today.AddDays(-1) && x.EndDate >= historyStart)
            .Select(x => new { x.UserId, x.StartDate, x.EndDate })
            .ToListAsync(cancellationToken);
        var dailyAttendance = historyDates
            .Select(date =>
            {
                var firstEntries = firstEntriesByDate.GetValueOrDefault(date) ?? new Dictionary<Guid, DateTime>();
                var dailyLateBoundaryUtc = TimeZoneInfo.ConvertTimeToUtc(
                    date.ToDateTime(shiftStart, DateTimeKind.Unspecified).AddMinutes(tolerance), timeZone);
                var dailyPresentSet = firstEntries.Keys.ToHashSet();
                var scheduledPersonnel = personnel
                    .Where(employee => IsWorkingDay(employee, date) || dailyPresentSet.Contains(employee.Id))
                    .ToArray();
                var scheduledPersonnelIds = scheduledPersonnel.Select(x => x.Id).ToHashSet();
                var dailyLateSet = firstEntries
                    .Where(x => scheduledPersonnelIds.Contains(x.Key) && x.Value > dailyLateBoundaryUtc)
                    .Select(x => x.Key)
                    .ToHashSet();
                var dailyLeaveSet = historicalLeaves
                    .Where(x => scheduledPersonnelIds.Contains(x.UserId) && x.StartDate <= date && x.EndDate >= date && !dailyPresentSet.Contains(x.UserId))
                    .Select(x => x.UserId)
                    .ToHashSet();

                return new DashboardDailyAttendanceDto(
                    date,
                    scheduledPersonnel.Length,
                    scheduledPersonnel.Where(x => dailyPresentSet.Contains(x.Id) && !dailyLateSet.Contains(x.Id)).ToArray(),
                    scheduledPersonnel.Where(x => dailyLateSet.Contains(x.Id)).ToArray(),
                    scheduledPersonnel.Where(x => dailyLeaveSet.Contains(x.Id)).ToArray(),
                    scheduledPersonnel.Where(x => !dailyPresentSet.Contains(x.Id) && !dailyLeaveSet.Contains(x.Id)).ToArray());
            })
            .ToArray();

        var movements = await (
            from log in context.AccessLogs.AsNoTracking()
            join user in context.Users.AsNoTracking() on log.UserId equals user.Id
            join zone in context.Zones.AsNoTracking() on log.ZoneId equals zone.Id into zones
            from zone in zones.DefaultIfEmpty()
            orderby log.LogDate descending
            select new { user.Name, user.Id, log.LogDate, log.LogType, ZoneName = zone == null ? "Bilinmeyen bölge" : zone.Name })
            .Take(5).ToListAsync(cancellationToken);

        var pendingLeaves = await context.LeaveRequests.AsNoTracking().Include(x => x.User)
            .Where(x => x.Status == LeaveRequestStatus.Pending)
            .OrderBy(x => x.CreatedAt).Take(3)
            .Select(x => new DashboardLeaveDto(x.Id, x.User.Name, x.StartDate, x.EndDate, x.LeaveType.ToString()))
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            today,
            totalPersonnel,
            presentPersonnel.Length,
            latePersonnel.Length,
            onLeavePersonnel.Length,
            missingRecordPersonnel.Length,
            pendingLeaveCount,
            dailyAttendance,
            presentPersonnel,
            latePersonnel,
            onLeavePersonnel,
            missingRecordPersonnel,
            movements.Select(x => new DashboardMovementDto(
                x.Name,
                $"FDM-{x.Id.ToString()[..6].ToUpperInvariant()}",
                TimeZoneInfo.ConvertTime(new DateTimeOffset(DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc)), timeZone),
                x.LogType,
                x.ZoneName)).ToArray(),
            pendingLeaves);
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}

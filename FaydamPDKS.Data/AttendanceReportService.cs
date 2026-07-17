using FaydamPDKS.Core.Attendance;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FaydamPDKS.Data;

public sealed class AttendanceReportService(
    AppDbContext context,
    IConfiguration configuration) : IAttendanceReportService
{
    private readonly AttendanceCalculator _calculator = new();

    public async Task<AttendanceReportDto> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (from == default || to == default || from > to) throw new ArgumentException("Geçerli bir tarih aralığı seçin.");
        if (to.DayNumber - from.DayNumber + 1 > 31) throw new ArgumentException("Rapor en fazla 31 günlük alınabilir.");

        var timeZone = ResolveTimeZone(configuration["Attendance:TimeZone"] ?? "Europe/Istanbul");
        var localStart = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).AddHours(-4);
        var localEnd = to.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).AddHours(8);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

        var employees = await context.Users.AsNoTracking().Include(x => x.Department).Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var employeeIds = employees.Select(x => x.Id).ToArray();
        var logs = await context.AccessLogs.AsNoTracking()
            .Where(x => employeeIds.Contains(x.UserId) && x.LogDate >= startUtc && x.LogDate < endUtc)
            .ToListAsync(cancellationToken);
        var shiftAssignments = await context.EmployeeShiftAssignments.AsNoTracking().Include(x => x.Shift)
            .Where(x => employeeIds.Contains(x.EmployeeId) && x.ValidFrom <= to && (!x.ValidTo.HasValue || x.ValidTo.Value >= from))
            .ToListAsync(cancellationToken);
        var corrections = await context.AttendanceCorrectionRequests.AsNoTracking()
            .Where(x => employeeIds.Contains(x.UserId) && x.WorkDate >= from && x.WorkDate <= to && x.Status == AttendanceCorrectionStatus.Approved)
            .OrderByDescending(x => x.ReviewedAt).ToListAsync(cancellationToken);
        var calendarDays = await context.WorkCalendarDays.AsNoTracking()
            .Where(x => x.Date >= from && x.Date <= to).ToListAsync(cancellationToken);
        var locationAssignments = await context.WorkLocationAssignments.AsNoTracking().Include(x => x.Days)
            .Where(x => employeeIds.Contains(x.UserId) && x.IsActive && x.StartDate <= to && (!x.EndDate.HasValue || x.EndDate.Value >= from))
            .ToListAsync(cancellationToken);
        var breakRecords = await context.BreakRecords.AsNoTracking()
            .Where(x => employeeIds.Contains(x.UserId) && x.EndedAt.HasValue && x.StartedAt >= new DateTimeOffset(startUtc) && x.StartedAt < new DateTimeOffset(endUtc))
            .ToListAsync(cancellationToken);

        var fallback = new ShiftDefinition(
            TimeOnly.Parse(configuration["Attendance:DefaultShiftStart"] ?? "09:00"),
            TimeOnly.Parse(configuration["Attendance:DefaultShiftEnd"] ?? "18:00"),
            ReadInt("Attendance:LateToleranceMinutes", 5),
            ReadInt("Attendance:EarlyLeaveToleranceMinutes", 5),
            ReadInt("Attendance:BreakMinutes", 60));
        var eventsByEmployee = logs.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.Select(log => new AttendanceEvent(
            log.UserId, new DateTimeOffset(DateTime.SpecifyKind(log.LogDate, DateTimeKind.Utc)),
            log.LogType.Equals("Giris", StringComparison.OrdinalIgnoreCase) ? AttendanceEventType.Entry : AttendanceEventType.Exit,
            log.DeviceEventId ?? log.Id.ToString())).ToArray());

        var rows = new List<AttendanceReportRowDto>();
        foreach (var employee in employees)
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var assignment = shiftAssignments.Where(x => x.EmployeeId == employee.Id && x.ValidFrom <= date &&
                (!x.ValidTo.HasValue || x.ValidTo.Value >= date) && x.Shift!.IsActive).OrderByDescending(x => x.ValidFrom).FirstOrDefault();
            var shift = assignment?.Shift is { } assigned
                ? new ShiftDefinition(assigned.StartsAt, assigned.EndsAt, assigned.LateToleranceMinutes, assigned.EarlyLeaveToleranceMinutes, assigned.BreakMinutes)
                : fallback;
            var correction = corrections.FirstOrDefault(x => x.UserId == employee.Id && x.WorkDate == date && x.CorrectionType == AttendanceCorrectionType.TimeCorrection);
            var rawEvents = eventsByEmployee.GetValueOrDefault(employee.Id) ?? [];
            var dayEvents = correction is null
                ? rawEvents
                : CorrectionEvents(employee.Id, correction, timeZone);
            var specialDay = calendarDays.Where(x => x.Date == date && (x.WorkplaceId == employee.WorkplaceId || x.WorkplaceId == null))
                .OrderByDescending(x => x.WorkplaceId.HasValue).FirstOrDefault();
            var isWorkingDay = specialDay is not null
                ? specialDay.DayType == CalendarDayType.WorkingDayOverride
                : date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
            var dayStartLocal = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var dayEndLocal = date.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var dayStartUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, timeZone));
            var dayEndUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(dayEndLocal, timeZone));
            var employeeBreaks = breakRecords.Where(x => x.UserId == employee.Id && x.StartedAt >= dayStartUtc && x.StartedAt < dayEndUtc).ToArray();
            int? actualBreakMinutes = employeeBreaks.Length == 0 ? null : employeeBreaks.Sum(x => Math.Max(0, (int)(x.EndedAt!.Value - x.StartedAt).TotalMinutes));
            var day = _calculator.Calculate(date, shift, dayEvents, timeZone, isWorkingDay, actualBreakMinutes);
            var calendarLabel = specialDay?.Name ?? (!isWorkingDay ? "Hafta tatili" : null);
            var hasActualQr = rawEvents.Any(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.OccurredAt, timeZone).DateTime) == date);
            var location = locationAssignments.Where(x => x.UserId == employee.Id && x.StartDate <= date && (!x.EndDate.HasValue || x.EndDate >= date)
                    && WorkLocationService.Applies(x.RecurrenceType, x.Days.Select(d => d.DayOfWeek), date))
                .OrderByDescending(x => x.LocationType == WorkLocationType.Field).ThenByDescending(x => x.CreatedAt).FirstOrDefault();
            if (isWorkingDay && !hasActualQr && correction is null && location is not null)
            {
                var expected = ExpectedMinutes(shift);
                rows.Add(new AttendanceReportRowDto(employee.Id, employee.EmployeeNumber, employee.Name, employee.Department?.Name ?? employee.DepartmentLegacy,
                    date, assignment?.Shift?.Name ?? "Varsayılan vardiya", location.LocationType == WorkLocationType.Field ? AttendanceStatus.FieldWork.ToString() : AttendanceStatus.RemoteWork.ToString(),
                    null, null, expected, expected, 0, 0, location.LocationType.ToString(), "WorkLocationPlan", true,
                    location.ProjectName ?? location.CustomerName ?? location.Reason));
                continue;
            }
            rows.Add(new AttendanceReportRowDto(employee.Id, employee.EmployeeNumber, employee.Name, employee.Department?.Name ?? employee.DepartmentLegacy,
                date, calendarLabel is null ? assignment?.Shift?.Name ?? "Varsayılan vardiya" : $"{calendarLabel} · {(assignment?.Shift?.Name ?? "Varsayılan vardiya")}", day.Status.ToString(), day.FirstEntry,
                day.LastExit, day.WorkedMinutes, day.ExpectedMinutes, day.LateMinutes, day.OvertimeMinutes, "Office", correction is null ? "QR" : "Correction", false, null));
        }
        return new AttendanceReportDto(from, to, rows);
    }

    private int ReadInt(string key, int fallback) =>
        int.TryParse(configuration[key], out var value) ? value : fallback;

    private static int ExpectedMinutes(ShiftDefinition shift)
    {
        var start = shift.StartsAt.ToTimeSpan();
        var end = shift.EndsAt.ToTimeSpan();
        if (end <= start) end = end.Add(TimeSpan.FromDays(1));
        return Math.Max(0, (int)(end - start).TotalMinutes - shift.BreakMinutes);
    }

    private static AttendanceEvent[] CorrectionEvents(Guid employeeId, AttendanceCorrectionRequest correction, TimeZoneInfo timeZone)
    {
        var entry = correction.WorkDate.ToDateTime(correction.RequestedEntry, DateTimeKind.Unspecified);
        var exitDate = correction.RequestedExit <= correction.RequestedEntry ? correction.WorkDate.AddDays(1) : correction.WorkDate;
        var exit = exitDate.ToDateTime(correction.RequestedExit, DateTimeKind.Unspecified);
        return
        [
            new(employeeId, new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(entry, timeZone)), AttendanceEventType.Entry, $"correction:{correction.Id}:entry"),
            new(employeeId, new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(exit, timeZone)), AttendanceEventType.Exit, $"correction:{correction.Id}:exit")
        ];
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}

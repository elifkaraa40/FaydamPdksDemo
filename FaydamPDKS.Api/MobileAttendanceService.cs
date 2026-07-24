using FaydamPDKS.Core.Attendance;
using FaydamPDKS.Core.DTOs.Attendance;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Api;

public sealed class MobileAttendanceService(
    IAccessLogRepository accessLogs,
    IShiftResolver shiftResolver,
    IAttendanceCorrectionRepository corrections,
    IWorkCalendarResolver workCalendar,
    IBreakService breaks,
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    TimeProvider timeProvider) : IAttendanceService
{
    private readonly AttendanceCalculator _calculator = new();

    public async Task<TodayAttendanceDto> GetTodayAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var timeZone = ResolveTimeZone(configuration["Attendance:TimeZone"] ?? "Europe/Istanbul");
        var now = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        var workDate = DateOnly.FromDateTime(now.DateTime);
        var shift = await ResolveShiftAsync(employeeId, workDate, cancellationToken);

        var localWindowStart = workDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localWindowEnd = workDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localWindowStart, timeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localWindowEnd, timeZone);
        var logs = await accessLogs.GetForUserAsync(employeeId, fromUtc, toUtc, cancellationToken);
        var rawEvents = logs.Select(x => new AttendanceEvent(
            employeeId,
            new DateTimeOffset(DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc)),
            x.LogType.Equals("Giris", StringComparison.OrdinalIgnoreCase)
                ? AttendanceEventType.Entry
                : AttendanceEventType.Exit,
            x.DeviceEventId ?? x.Id.ToString())).ToArray();
        var firstEntry = rawEvents.FirstOrDefault(x => x.Type == AttendanceEventType.Entry);
        var todayEvents = firstEntry is null
            ? Array.Empty<AttendanceEvent>()
            : rawEvents.Where(x => x.OccurredAt >= firstEntry.OccurredAt).ToArray();
        var correction = (await corrections.GetApprovedAsync(employeeId, workDate, workDate, cancellationToken)).FirstOrDefault();
        var events = correction is null ? todayEvents : CorrectionEvents(employeeId, correction, timeZone);

        var calendar = await workCalendar.ResolveAsync(employeeId, workDate, cancellationToken);
        var breakMinutes = await GetBreakMinutesAsync(employeeId, workDate, timeZone, cancellationToken);
        var result = _calculator.Calculate(workDate, shift, events, timeZone, calendar.IsWorkingDay, breakMinutes);
        return new TodayAttendanceDto(
            result.WorkDate,
            result.Status.ToString(),
            result.FirstEntry,
            result.LastExit,
            result.WorkedMinutes,
            result.ExpectedMinutes,
            result.LateMinutes,
            result.OvertimeMinutes);
    }

    public async Task<IReadOnlyList<TodayAttendanceDto>> GetRangeAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from == default || to == default) throw new ArgumentException("Başlangıç ve bitiş tarihleri zorunludur.");
        if (from > to) throw new ArgumentException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        if (to.DayNumber - from.DayNumber + 1 > 90) throw new ArgumentException("En fazla 90 günlük aralık sorgulanabilir.");

        var timeZone = ResolveTimeZone(configuration["Attendance:TimeZone"] ?? "Europe/Istanbul");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
        if (to > today) throw new ArgumentException("Gelecek tarihli puantaj geçmişi sorgulanamaz.");
        var localStart = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).AddHours(-4);
        var localEnd = to.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).AddHours(8);
        var logs = await accessLogs.GetForUserAsync(
            employeeId,
            TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone),
            cancellationToken);
        var events = logs.Select(x => new AttendanceEvent(
            employeeId,
            new DateTimeOffset(DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc)),
            x.LogType.Equals("Giris", StringComparison.OrdinalIgnoreCase) ? AttendanceEventType.Entry : AttendanceEventType.Exit,
            x.DeviceEventId ?? x.Id.ToString())).ToArray();
        var approvedCorrections = (await corrections.GetApprovedAsync(employeeId, from, to, cancellationToken))
            .GroupBy(x => x.WorkDate).ToDictionary(x => x.Key, x => x.First());

        var result = new List<TodayAttendanceDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var shift = await ResolveShiftAsync(employeeId, date, cancellationToken);
            var dayEvents = approvedCorrections.TryGetValue(date, out var correction)
                ? CorrectionEvents(employeeId, correction, timeZone)
                : events;
            var calendar = await workCalendar.ResolveAsync(employeeId, date, cancellationToken);
            var breakMinutes = await GetBreakMinutesAsync(employeeId, date, timeZone, cancellationToken);
            var day = _calculator.Calculate(date, shift, dayEvents, timeZone, calendar.IsWorkingDay, breakMinutes);
            result.Add(new TodayAttendanceDto(
                day.WorkDate, day.Status.ToString(), day.FirstEntry, day.LastExit,
                day.WorkedMinutes, day.ExpectedMinutes, day.LateMinutes, day.OvertimeMinutes));
        }
        return result;
    }

    public async Task<IReadOnlyList<QrAttendanceHistoryDto>> GetQrHistoryAsync(
        Guid employeeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var logs = await accessLogs.GetRecentQrForUserAsync(employeeId, Math.Clamp(limit, 1, 100), cancellationToken);
        return logs.Select(x => new QrAttendanceHistoryDto(
            x.Id,
            new DateTimeOffset(DateTime.SpecifyKind(x.LogDate, DateTimeKind.Utc)),
            x.LogType.Equals("Giris", StringComparison.OrdinalIgnoreCase) ? "Entry" : "Exit"))
            .ToArray();
    }

    public async Task<bool> AddEventAsync(Guid employeeId, CreateAttendanceEventRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (request.OccurredAt > now.AddMinutes(5) || request.OccurredAt < now.AddDays(-7))
            throw new ArgumentOutOfRangeException(nameof(request.OccurredAt), "Olay zamanı kabul edilen aralığın dışında.");

        var deviceEventId = request.DeviceEventId.Trim();
        if (await accessLogs.DeviceEventExistsAsync(deviceEventId, cancellationToken)) return false;

        await accessLogs.AddAsync(new AccessLog
        {
            UserId = employeeId,
            ZoneId = request.ZoneId,
            LogDate = request.OccurredAt.UtcDateTime,
            LogType = request.EventType == AttendanceEventType.Entry ? "Giris" : "Cikis",
            DeviceEventId = deviceEventId,
            Source = "Mobile"
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private ShiftDefinition CreateDefaultShift() => new(
        TimeOnly.Parse(configuration["Attendance:DefaultShiftStart"] ?? "09:00"),
        TimeOnly.Parse(configuration["Attendance:DefaultShiftEnd"] ?? "18:00"),
        configuration.GetValue("Attendance:LateToleranceMinutes", 5),
        configuration.GetValue("Attendance:EarlyLeaveToleranceMinutes", 5),
        configuration.GetValue("Attendance:BreakMinutes", 60));

    private Task<int?> GetBreakMinutesAsync(Guid employeeId, DateOnly date, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = date.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return breaks.GetCompletedMinutesAsync(employeeId,
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone)),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone)), cancellationToken);
    }

    private async Task<ShiftDefinition> ResolveShiftAsync(Guid employeeId, DateOnly workDate, CancellationToken cancellationToken) =>
        await shiftResolver.ResolveAsync(employeeId, workDate, cancellationToken) ?? CreateDefaultShift();

    private static AttendanceEvent[] CorrectionEvents(Guid employeeId, AttendanceCorrectionRequest correction, TimeZoneInfo timeZone)
    {
        var entryLocal = correction.WorkDate.ToDateTime(correction.RequestedEntry, DateTimeKind.Unspecified);
        var exitDate = correction.RequestedExit <= correction.RequestedEntry ? correction.WorkDate.AddDays(1) : correction.WorkDate;
        var exitLocal = exitDate.ToDateTime(correction.RequestedExit, DateTimeKind.Unspecified);
        return
        [
            new(employeeId, new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(entryLocal, timeZone)), AttendanceEventType.Entry, $"correction:{correction.Id}:entry"),
            new(employeeId, new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(exitLocal, timeZone)), AttendanceEventType.Exit, $"correction:{correction.Id}:exit")
        ];
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}

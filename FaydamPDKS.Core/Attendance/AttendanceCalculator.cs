namespace FaydamPDKS.Core.Attendance;

public sealed class AttendanceCalculator
{
    public DailyAttendanceResult Calculate(
        DateOnly workDate,
        ShiftDefinition shift,
        IEnumerable<AttendanceEvent> events,
        TimeZoneInfo timeZone,
        bool isWorkingDay = true)
    {
        ArgumentNullException.ThrowIfNull(shift);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(timeZone);

        var shiftStartLocal = workDate.ToDateTime(shift.StartsAt, DateTimeKind.Unspecified);
        var endDate = shift.CrossesMidnight ? workDate.AddDays(1) : workDate;
        var shiftEndLocal = endDate.ToDateTime(shift.EndsAt, DateTimeKind.Unspecified);
        var shiftStart = new DateTimeOffset(shiftStartLocal, timeZone.GetUtcOffset(shiftStartLocal));
        var shiftEnd = new DateTimeOffset(shiftEndLocal, timeZone.GetUtcOffset(shiftEndLocal));

        // Geniş pencere, vardiya öncesi/sonrası basımları ve gece vardiyasını kapsar.
        var windowStart = shiftStart.AddHours(-4);
        var windowEnd = shiftEnd.AddHours(8);
        var ordered = events
            .Where(x => x.OccurredAt >= windowStart && x.OccurredAt <= windowEnd)
            .OrderBy(x => x.OccurredAt)
            .ToArray();

        var firstEntry = ordered.FirstOrDefault(x => x.Type == AttendanceEventType.Entry)?.OccurredAt;
        var lastExit = ordered.LastOrDefault(x => x.Type == AttendanceEventType.Exit)?.OccurredAt;
        var expected = isWorkingDay ? Math.Max(0, (int)(shiftEnd - shiftStart).TotalMinutes - shift.BreakMinutes) : 0;

        if (firstEntry is null && lastExit is null)
            return Empty(workDate, isWorkingDay ? AttendanceStatus.NoRecord : AttendanceStatus.NonWorkingDay, expected);
        if (firstEntry is null)
            return new(workDate, AttendanceStatus.MissingEntry, null, lastExit, 0, expected, 0, 0, 0);
        if (lastExit is null || lastExit <= firstEntry)
            return new(workDate, AttendanceStatus.MissingExit, firstEntry, lastExit, 0, expected, 0, 0, 0);

        var worked = Math.Max(0, (int)(lastExit.Value - firstEntry.Value).TotalMinutes - shift.BreakMinutes);
        var late = isWorkingDay ? Math.Max(0, (int)(firstEntry.Value - shiftStart).TotalMinutes - shift.LateToleranceMinutes) : 0;
        var early = isWorkingDay ? Math.Max(0, (int)(shiftEnd - lastExit.Value).TotalMinutes - shift.EarlyLeaveToleranceMinutes) : 0;
        var overtime = Math.Max(0, worked - expected);

        return new(workDate, AttendanceStatus.Complete, firstEntry, lastExit, worked, expected, late, early, overtime);
    }

    private static DailyAttendanceResult Empty(DateOnly date, AttendanceStatus status, int expected) =>
        new(date, status, null, null, 0, expected, 0, 0, 0);
}

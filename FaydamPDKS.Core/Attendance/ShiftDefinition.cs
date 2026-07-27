namespace FaydamPDKS.Core.Attendance;

public sealed record ShiftDefinition
{
    public ShiftDefinition(
        TimeOnly startsAt,
        TimeOnly endsAt,
        int lateToleranceMinutes = 0,
        int earlyLeaveToleranceMinutes = 0,
        int breakMinutes = 0,
        TimeOnly? scheduledBreakStart = null,
        TimeOnly? scheduledBreakEnd = null)
    {
        if (lateToleranceMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(lateToleranceMinutes));
        if (earlyLeaveToleranceMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(earlyLeaveToleranceMinutes));
        if (breakMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(breakMinutes));

        StartsAt = startsAt;
        EndsAt = endsAt;
        LateToleranceMinutes = lateToleranceMinutes;
        EarlyLeaveToleranceMinutes = earlyLeaveToleranceMinutes;
        BreakMinutes = breakMinutes;
        ScheduledBreakStart = scheduledBreakStart;
        ScheduledBreakEnd = scheduledBreakEnd;
    }

    public TimeOnly StartsAt { get; }
    public TimeOnly EndsAt { get; }
    public int LateToleranceMinutes { get; }
    public int EarlyLeaveToleranceMinutes { get; }
    public int BreakMinutes { get; }
    public TimeOnly? ScheduledBreakStart { get; }
    public TimeOnly? ScheduledBreakEnd { get; }

    public bool CrossesMidnight => EndsAt <= StartsAt;

    public ShiftDefinition ShortenForHoliday(TimeOnly holidayStartsAt)
    {
        if (CrossesMidnight || holidayStartsAt <= StartsAt || holidayStartsAt >= EndsAt)
            return this;

        var effectiveEnd = holidayStartsAt;
        var effectiveBreakMinutes = 0;
        if (ScheduledBreakStart.HasValue && ScheduledBreakEnd.HasValue)
        {
            if (ScheduledBreakStart.Value < holidayStartsAt && ScheduledBreakEnd.Value > holidayStartsAt)
                effectiveEnd = ScheduledBreakStart.Value;
            else if (ScheduledBreakEnd.Value <= holidayStartsAt)
                effectiveBreakMinutes = BreakMinutes;
        }

        return new ShiftDefinition(
            StartsAt,
            effectiveEnd,
            LateToleranceMinutes,
            EarlyLeaveToleranceMinutes,
            effectiveBreakMinutes);
    }
}

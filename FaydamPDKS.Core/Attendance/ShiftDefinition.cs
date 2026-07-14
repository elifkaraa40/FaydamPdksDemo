namespace FaydamPDKS.Core.Attendance;

public sealed record ShiftDefinition
{
    public ShiftDefinition(
        TimeOnly startsAt,
        TimeOnly endsAt,
        int lateToleranceMinutes = 0,
        int earlyLeaveToleranceMinutes = 0,
        int breakMinutes = 0)
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
    }

    public TimeOnly StartsAt { get; }
    public TimeOnly EndsAt { get; }
    public int LateToleranceMinutes { get; }
    public int EarlyLeaveToleranceMinutes { get; }
    public int BreakMinutes { get; }

    public bool CrossesMidnight => EndsAt <= StartsAt;
}

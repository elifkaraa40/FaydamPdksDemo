using FaydamPDKS.Core.Attendance;
using Xunit;

namespace FaydamPDKS.Tests.Attendance;

public sealed class AttendanceCalculatorTests
{
    private static readonly Guid EmployeeId = Guid.Parse("3c52690b-d15a-4545-86ec-c431ce71efc7");
    private static readonly TimeZoneInfo Istanbul = ResolveIstanbulTimeZone();
    private readonly AttendanceCalculator _calculator = new();

    [Fact]
    public void Calculates_late_and_overtime_for_day_shift()
    {
        var date = new DateOnly(2026, 7, 14);
        var shift = new ShiftDefinition(new TimeOnly(9, 0), new TimeOnly(18, 0), lateToleranceMinutes: 5, breakMinutes: 60);
        var events = new[]
        {
            Event(date, 9, 12, AttendanceEventType.Entry),
            Event(date, 19, 12, AttendanceEventType.Exit)
        };

        var result = _calculator.Calculate(date, shift, events, Istanbul);

        Assert.Equal(AttendanceStatus.Complete, result.Status);
        Assert.Equal(540, result.WorkedMinutes);
        Assert.Equal(480, result.ExpectedMinutes);
        Assert.Equal(7, result.LateMinutes);
        Assert.Equal(60, result.OvertimeMinutes);
    }

    [Fact]
    public void Supports_shift_crossing_midnight()
    {
        var date = new DateOnly(2026, 7, 14);
        var shift = new ShiftDefinition(new TimeOnly(22, 0), new TimeOnly(6, 0));
        var events = new[]
        {
            Event(date, 21, 58, AttendanceEventType.Entry),
            Event(date.AddDays(1), 6, 3, AttendanceEventType.Exit)
        };

        var result = _calculator.Calculate(date, shift, events, Istanbul);

        Assert.Equal(AttendanceStatus.Complete, result.Status);
        Assert.Equal(485, result.WorkedMinutes);
        Assert.Equal(5, result.OvertimeMinutes);
    }

    [Fact]
    public void Marks_record_without_exit_as_incomplete()
    {
        var date = new DateOnly(2026, 7, 14);
        var shift = new ShiftDefinition(new TimeOnly(9, 0), new TimeOnly(18, 0));

        var result = _calculator.Calculate(
            date,
            shift,
            new[] { Event(date, 8, 59, AttendanceEventType.Entry) },
            Istanbul);

        Assert.Equal(AttendanceStatus.MissingExit, result.Status);
        Assert.Equal(0, result.WorkedMinutes);
    }

    [Fact]
    public void Non_working_day_without_events_has_zero_expected_minutes()
    {
        var date = new DateOnly(2026, 7, 19);
        var result = _calculator.Calculate(date, new ShiftDefinition(new TimeOnly(9, 0), new TimeOnly(18, 0), breakMinutes: 60), [], Istanbul, false);
        Assert.Equal(AttendanceStatus.NonWorkingDay, result.Status);
        Assert.Equal(0, result.ExpectedMinutes);
        Assert.Equal(0, result.MissingMinutes);
    }

    [Fact]
    public void Work_on_non_working_day_is_overtime_without_lateness()
    {
        var date = new DateOnly(2026, 7, 19);
        var events = new[] { Event(date, 10, 0, AttendanceEventType.Entry), Event(date, 15, 0, AttendanceEventType.Exit) };
        var result = _calculator.Calculate(date, new ShiftDefinition(new TimeOnly(9, 0), new TimeOnly(18, 0), breakMinutes: 60), events, Istanbul, false);
        Assert.Equal(240, result.WorkedMinutes);
        Assert.Equal(240, result.OvertimeMinutes);
        Assert.Equal(0, result.LateMinutes);
    }

    [Fact]
    public void Uses_recorded_break_duration_when_available()
    {
        var date = new DateOnly(2026, 7, 14);
        var events = new[] { Event(date, 9, 0, AttendanceEventType.Entry), Event(date, 18, 0, AttendanceEventType.Exit) };

        var result = _calculator.Calculate(date,
            new ShiftDefinition(new TimeOnly(9, 0), new TimeOnly(18, 0), breakMinutes: 60),
            events, Istanbul, actualBreakMinutes: 30);

        Assert.Equal(450, result.WorkedMinutes);
        Assert.Equal(480, result.ExpectedMinutes);
        Assert.Equal(0, result.OvertimeMinutes);
    }

    private static AttendanceEvent Event(DateOnly date, int hour, int minute, AttendanceEventType type)
    {
        var local = date.ToDateTime(new TimeOnly(hour, minute), DateTimeKind.Unspecified);
        return new AttendanceEvent(EmployeeId, new DateTimeOffset(local, Istanbul.GetUtcOffset(local)), type, Guid.NewGuid().ToString());
    }

    private static TimeZoneInfo ResolveIstanbulTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}

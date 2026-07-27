namespace FaydamPDKS.Core.Interfaces;

public sealed record OfficialHoliday(
    DateOnly Date,
    string Name,
    bool IsHalfDay,
    string Source);

public sealed record OfficialHolidayCalendar(
    int Year,
    IReadOnlyList<OfficialHoliday> Holidays,
    bool IsComplete,
    bool UsedFallback,
    string? Warning = null);

public interface IPublicHolidayProvider
{
    Task<OfficialHolidayCalendar> GetTurkeyCalendarAsync(
        int year,
        CancellationToken cancellationToken = default);
}

public sealed record PublicHolidaySyncResult(
    int Year,
    int Added,
    int Updated,
    bool IsComplete,
    DateTimeOffset? LastSuccessfulAt,
    string? Warning);

public interface IPublicHolidaySyncService
{
    Task<PublicHolidaySyncResult> SyncYearAsync(
        int year,
        CancellationToken cancellationToken = default);
}

namespace FaydamPDKS.Core.Interfaces;

public sealed record WorkdayResolution(
    bool IsWorkingDay,
    string? Name,
    double WorkdayWeight = 1,
    TimeOnly? WorkingUntil = null);

public interface IWorkCalendarResolver
{
    Task<WorkdayResolution> ResolveAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
}

namespace FaydamPDKS.Core.Interfaces;

public sealed record WorkdayResolution(bool IsWorkingDay, string? Name);

public interface IWorkCalendarResolver
{
    Task<WorkdayResolution> ResolveAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
}

namespace FaydamPDKS.Core.Exceptions;

public sealed class LeaveOverlapException(
    DateOnly conflictingStartDate,
    DateOnly conflictingEndDate) : InvalidOperationException("LEAVE_OVERLAP")
{
    public DateOnly ConflictingStartDate { get; } = conflictingStartDate;
    public DateOnly ConflictingEndDate { get; } = conflictingEndDate;
}

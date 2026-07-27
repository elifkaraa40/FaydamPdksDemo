namespace FaydamPDKS.Core.Exceptions;

public sealed class WorkLocationOverlapException(
    DateOnly conflictingStartDate,
    DateOnly conflictingEndDate,
    string conflictingRecordType)
    : InvalidOperationException("WORK_LOCATION_OVERLAP")
{
    public DateOnly ConflictingStartDate { get; } = conflictingStartDate;
    public DateOnly ConflictingEndDate { get; } = conflictingEndDate;
    public string ConflictingRecordType { get; } = conflictingRecordType;
}

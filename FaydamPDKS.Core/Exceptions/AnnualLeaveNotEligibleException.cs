namespace FaydamPDKS.Core.Exceptions;

public sealed class AnnualLeaveNotEligibleException(DateOnly eligibleOn)
    : InvalidOperationException("ANNUAL_LEAVE_NOT_ELIGIBLE")
{
    public DateOnly EligibleOn { get; } = eligibleOn;
}

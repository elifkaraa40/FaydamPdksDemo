namespace FaydamPDKS.Core.Exceptions;

public sealed class AnnualLeaveException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}

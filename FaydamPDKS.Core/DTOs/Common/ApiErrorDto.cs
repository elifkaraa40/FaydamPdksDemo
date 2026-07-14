namespace FaydamPDKS.Core.DTOs.Common;

public sealed record ApiErrorDto(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? TraceId = null);

namespace FaydamPDKS.Core.DTOs;

public sealed record PasswordResetEmailTicket(string RecipientEmail, string RecipientName, string RawToken);

public sealed record PasswordResetRequestListItemDto(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    string EmployeeNumber,
    string Email,
    DateTimeOffset RequestedAt);

public sealed record PasswordResetReviewResult(bool Found, string? TemporaryPassword = null);

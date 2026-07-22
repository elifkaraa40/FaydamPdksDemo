using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed record MobileProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber,
    string? ProfileImageUrl,
    bool IsEmailNotificationEnabled,
    bool IsSmsNotificationEnabled,
    string? EmployeeNumber,
    string? DepartmentName,
    string? WorkplaceName,
    DateOnly? HireDate);

public sealed record UpdateMobileProfileDto(
    [StringLength(30)] string? PhoneNumber,
    bool IsEmailNotificationEnabled,
    bool IsSmsNotificationEnabled);

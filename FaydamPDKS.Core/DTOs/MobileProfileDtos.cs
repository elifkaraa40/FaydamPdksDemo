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

public sealed record ChangeMobilePasswordDto(
    [Required, StringLength(FaydamPDKS.Core.Security.PasswordPolicy.MaximumLength)] string CurrentPassword,
    [Required, StringLength(FaydamPDKS.Core.Security.PasswordPolicy.MaximumLength,
        MinimumLength = FaydamPDKS.Core.Security.PasswordPolicy.MinimumLength,
        ErrorMessage = FaydamPDKS.Core.Security.PasswordPolicy.RequirementMessage)] string NewPassword,
    [Required, StringLength(FaydamPDKS.Core.Security.PasswordPolicy.MaximumLength,
        MinimumLength = FaydamPDKS.Core.Security.PasswordPolicy.MinimumLength,
        ErrorMessage = FaydamPDKS.Core.Security.PasswordPolicy.RequirementMessage)] string NewPasswordConfirmation);

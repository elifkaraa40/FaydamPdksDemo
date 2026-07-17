using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record PhoneRegistrationRequest(
    [Required, StringLength(100, MinimumLength = 3)] string FullName,
    [Required] string PhoneNumber,
    [Required, StringLength(FaydamPDKS.Core.Security.PasswordPolicy.MaximumLength,
        MinimumLength = FaydamPDKS.Core.Security.PasswordPolicy.MinimumLength,
        ErrorMessage = FaydamPDKS.Core.Security.PasswordPolicy.RequirementMessage)] string Password,
    string? DeviceName);

public sealed record PhonePasswordLoginRequest(
    [Required] string PhoneNumber,
    [Required] string Password,
    string? DeviceName);

using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record EmailRegistrationRequest(
    [Required, StringLength(100, MinimumLength = 3)] string FullName,
    [Required, EmailAddress, StringLength(100)] string Email,
    [Required, StringLength(FaydamPDKS.Core.Security.PasswordPolicy.MaximumLength,
        MinimumLength = FaydamPDKS.Core.Security.PasswordPolicy.MinimumLength,
        ErrorMessage = FaydamPDKS.Core.Security.PasswordPolicy.RequirementMessage)] string Password,
    string? DeviceName);

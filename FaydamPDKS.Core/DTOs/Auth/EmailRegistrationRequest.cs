using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record EmailRegistrationRequest(
    [Required, StringLength(100, MinimumLength = 3)] string FullName,
    [Required, EmailAddress, StringLength(100)] string Email,
    [Required, StringLength(FaydamPDKS.Core.Security.PasswordPolicy.MaximumLength,
        MinimumLength = FaydamPDKS.Core.Security.PasswordPolicy.MinimumLength,
        ErrorMessage = FaydamPDKS.Core.Security.PasswordPolicy.RequirementMessage)] string Password,
    [Required(ErrorMessage = "İşe giriş tarihi zorunludur.")] DateOnly? HireDate,
    [Required(ErrorMessage = "Doğum tarihi zorunludur.")] DateOnly? BirthDate,
    [Required, StringLength(200, MinimumLength = 16)] string DeviceId,
    string? DeviceName);

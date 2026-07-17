using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record MobileLoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    string? DeviceName = null);

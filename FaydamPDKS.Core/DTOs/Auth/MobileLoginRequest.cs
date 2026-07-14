using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record MobileLoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    string? DeviceName = null);

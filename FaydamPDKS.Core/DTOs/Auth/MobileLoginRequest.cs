using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record MobileLoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [Required, StringLength(200, MinimumLength = 16)] string DeviceId,
    [StringLength(150)] string? DeviceName = null);

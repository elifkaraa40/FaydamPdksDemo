using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs.Auth;

public sealed record MobilePasswordRecoveryRequest(
    [Required(ErrorMessage = "E-posta adresi zorunludur."), EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    string Email);

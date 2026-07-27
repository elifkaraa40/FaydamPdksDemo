using System.ComponentModel.DataAnnotations;
using FaydamPDKS.Core.Security;

namespace FaydamPDKS.Web.Models;

public sealed class PasswordRecoveryRequestModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur."), EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    public bool EmailResetAvailable { get; set; }
}

public sealed class ResetPasswordModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(PasswordPolicy.MaximumLength, MinimumLength = PasswordPolicy.MinimumLength,
        ErrorMessage = PasswordPolicy.RequirementMessage), DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Parolalar aynı olmalıdır."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Oturumu açık tut")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

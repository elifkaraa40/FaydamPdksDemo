using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FaydamPDKS.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/phone-auth")]
public sealed class MobilePhoneAuthController(PhoneAuthService phoneAuth) : ControllerBase
{
    [HttpPost("register"), EnableRateLimiting("mobile-auth")]
    public async Task<IActionResult> Register(PhoneRegistrationRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await phoneAuth.RegisterAsync(request, cancellationToken)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            var message = ex.Message == "PHONE_ALREADY_REGISTERED"
                ? "Bu telefon numarasıyla daha önce kayıt oluşturulmuş. Giriş ekranını kullanın."
                : ex is ArgumentException ? ex.Message : "Kayıt oluşturulamadı.";
            return BadRequest(Error(ex.Message, message));
        }
    }

    [HttpPost("login"), EnableRateLimiting("mobile-auth")]
    public async Task<IActionResult> Login(PhonePasswordLoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await phoneAuth.LoginAsync(request, cancellationToken);
            return response is null
                ? Unauthorized(Error("INVALID_CREDENTIALS", "Telefon numarası veya parola hatalı."))
                : Ok(response);
        }
        catch (ArgumentException ex) { return BadRequest(Error("INVALID_PHONE", ex.Message)); }
    }

    private ApiErrorDto Error(string code, string message) => new(code, message, TraceId: HttpContext.TraceIdentifier);
}

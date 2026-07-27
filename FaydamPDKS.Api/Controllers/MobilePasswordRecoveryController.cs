using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FaydamPDKS.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth/password-recovery")]
[Produces("application/json")]
public sealed class MobilePasswordRecoveryController(
    IPasswordResetService passwordResets,
    MobilePasswordResetEmailSender emailSender,
    ILogger<MobilePasswordRecoveryController> logger) : ControllerBase
{
    [HttpGet("options")]
    public IActionResult Options() => Ok(new { emailResetAvailable = emailSender.IsConfigured });

    [HttpPost("manager-request")]
    [EnableRateLimiting("mobile-auth")]
    public async Task<IActionResult> ManagerRequest(MobilePasswordRecoveryRequest request, CancellationToken cancellationToken)
    {
        await passwordResets.RequestManagerResetAsync(request.Email, cancellationToken);
        return Accepted(new { message = "Hesap bulunursa şifre sıfırlama talebi yöneticilere iletilecektir." });
    }

    [HttpPost("email-request")]
    [EnableRateLimiting("mobile-auth")]
    public async Task<IActionResult> EmailRequest(MobilePasswordRecoveryRequest request, CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiErrorDto(
                "SMTP_NOT_CONFIGURED", "E-postayla sıfırlama henüz kullanıma açılmadı.", TraceId: HttpContext.TraceIdentifier));

        var ticket = await passwordResets.CreateEmailResetAsync(request.Email, cancellationToken);
        if (ticket is not null)
        {
            try { await emailSender.SendAsync(ticket, cancellationToken); }
            catch (Exception exception)
            {
                logger.LogError(exception, "Mobil şifre sıfırlama e-postası gönderilemedi. TraceId: {TraceId}", HttpContext.TraceIdentifier);
            }
        }
        return Accepted(new { message = "Hesap bulunursa şifre sıfırlama bağlantısı e-posta adresine gönderilecektir." });
    }
}

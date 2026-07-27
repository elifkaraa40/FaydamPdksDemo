using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace FaydamPDKS.Api.Controllers;

[ApiController, Authorize]
[Route("api/v1/qr-attendance")]
[Produces("application/json")]
public sealed class MobileAttendanceQrController(
    IAttendanceQrService qrCodes,
    IMobileAuthService auth) : ControllerBase
{
    [HttpPost("scan")]
    [EnableRateLimiting("qr-scan")]
    [ProducesResponseType<ScanAttendanceQrResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Scan(
        ScanAttendanceQrRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
                User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
            return Unauthorized(Error("UNAUTHENTICATED", "Geçerli oturum bulunamadı."));

        if (!Guid.TryParse(User.FindFirstValue("sid"), out var sessionId))
            return StatusCode(
                StatusCodes.Status403Forbidden,
                Error(
                    "DEVICE_SESSION_REQUIRED",
                    "Cihaz oturumu doğrulanamadı. Lütfen tekrar giriş yapın."));

        var deviceSession = await auth.ValidateDeviceSessionAsync(
            userId, sessionId, request.DeviceId, cancellationToken);
        if (!deviceSession.IsValid)
        {
            var message = deviceSession.ErrorCode == "DEVICE_MISMATCH"
                ? "Bu QR işlemi giriş yapılan cihazdan gönderilmediği için reddedildi."
                : "Bu cihaz oturumu başka bir cihazdan giriş yapıldığı veya çıkış yapıldığı için kapatıldı.";
            return StatusCode(
                StatusCodes.Status403Forbidden,
                Error(deviceSession.ErrorCode ?? "DEVICE_SESSION_REJECTED", message));
        }

        try
        {
            var result = await qrCodes.ScanAsync(userId, request, cancellationToken);
            return result is null
                ? BadRequest(Error(
                    "INVALID_OR_INACTIVE_QR",
                    "QR kod geçersiz, yenilenmiş veya kullanım dışı."))
                : StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE_EVENT")
        {
            return Conflict(Error(
                "DUPLICATE_EVENT",
                "Bu cihaz olayı daha önce kaydedildi."));
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE_TRANSITION")
        {
            return Conflict(Error(
                "DUPLICATE_TRANSITION",
                "Aynı geçiş türü art arda okutulamaz."));
        }
    }

    private ApiErrorDto Error(string code, string message) =>
        new(code, message, TraceId: HttpContext.TraceIdentifier);
}

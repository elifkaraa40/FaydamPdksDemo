using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Api.Controllers;

[ApiController, Authorize]
[Route("api/v1/notifications")]
[Produces("application/json")]
public sealed class MobileNotificationsController(IMobileNotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] string? language, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        return Ok(await notifications.GetMineAsync(userId, language, cancellationToken));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        return await notifications.MarkReadAsync(userId, id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        return Ok(new { count = await notifications.GetUnreadCountAsync(userId, cancellationToken) });
    }

    [HttpPut("device")]
    public async Task<IActionResult> RegisterDevice(RegisterPushDeviceDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId) || !TryGetSessionId(out var sessionId)) return UnauthorizedError();
        await notifications.RegisterPushDeviceAsync(userId, sessionId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("device")]
    public async Task<IActionResult> UnregisterDevice(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId) || !TryGetSessionId(out var sessionId)) return UnauthorizedError();
        await notifications.UnregisterPushDeviceAsync(userId, sessionId, cancellationToken);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private bool TryGetSessionId(out Guid sessionId) =>
        Guid.TryParse(User.FindFirstValue("sid"), out sessionId);

    private UnauthorizedObjectResult UnauthorizedError() =>
        Unauthorized(new ApiErrorDto("UNAUTHENTICATED", "Geçerli oturum bulunamadı.", TraceId: HttpContext.TraceIdentifier));
}

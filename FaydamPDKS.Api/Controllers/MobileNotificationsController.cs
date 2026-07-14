using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Api.Controllers;

[ApiController, Authorize]
[Route("api/v1/notifications")]
[Produces("application/json")]
public sealed class MobileNotificationsController(IMobileNotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        return Ok(await notifications.GetMineAsync(userId, cancellationToken));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        return await notifications.MarkReadAsync(userId, id, cancellationToken) ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private UnauthorizedObjectResult UnauthorizedError() =>
        Unauthorized(new ApiErrorDto("UNAUTHENTICATED", "Geçerli oturum bulunamadı.", TraceId: HttpContext.TraceIdentifier));
}

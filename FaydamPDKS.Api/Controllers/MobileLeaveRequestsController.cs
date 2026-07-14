using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Api.Controllers;

[ApiController, Authorize]
[Route("api/v1/leave-requests")]
[Produces("application/json")]
public sealed class MobileLeaveRequestsController(ILeaveRequestService leaveRequests) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        return Ok(await leaveRequests.GetMineAsync(userId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLeaveRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        try
        {
            var created = await leaveRequests.CreateAsync(userId, request, cancellationToken);
            return Created($"/api/v1/leave-requests/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Error("INVALID_LEAVE_REQUEST", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(Error("OVERLAPPING_LEAVE_REQUEST", ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        try
        {
            return await leaveRequests.CancelAsync(userId, id, cancellationToken) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(Error("LEAVE_REQUEST_NOT_CANCELLABLE", ex.Message));
        }
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    private ApiErrorDto Error(string code, string message) => new(code, message, TraceId: HttpContext.TraceIdentifier);
    private UnauthorizedObjectResult UnauthorizedError() => Unauthorized(Error("UNAUTHENTICATED", "Geçerli oturum bulunamadı."));
}

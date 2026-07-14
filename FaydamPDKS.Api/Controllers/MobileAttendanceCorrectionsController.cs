using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Api.Controllers;

[ApiController, Authorize]
[Route("api/v1/attendance-corrections")]
[Produces("application/json")]
public sealed class MobileAttendanceCorrectionsController(IAttendanceCorrectionService corrections) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        TryGetUserId(out var userId) ? Ok(await corrections.GetMineAsync(userId, cancellationToken)) : Unauthorized(Error("UNAUTHENTICATED", "Geçerli oturum bulunamadı."));

    [HttpPost]
    public async Task<IActionResult> Create(CreateAttendanceCorrectionDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(Error("UNAUTHENTICATED", "Geçerli oturum bulunamadı."));
        try
        {
            var created = await corrections.CreateAsync(userId, request, cancellationToken);
            return Created($"/api/v1/attendance-corrections/{created.Id}", created);
        }
        catch (ArgumentException ex) { return BadRequest(Error("INVALID_CORRECTION_REQUEST", ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(Error("PENDING_CORRECTION_EXISTS", ex.Message)); }
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    private ApiErrorDto Error(string code, string message) => new(code, message, TraceId: HttpContext.TraceIdentifier);
}

using FaydamPDKS.Core.DTOs.Attendance;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/attendance")]
[Produces("application/json")]
public sealed class MobileAttendanceController(IAttendanceService attendance) : ControllerBase
{
    [HttpGet("today")]
    [ProducesResponseType<TodayAttendanceDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Today(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        return Ok(await attendance.GetTodayAsync(userId, cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TodayAttendanceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Range([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        try
        {
            return Ok(await attendance.GetRangeAsync(userId, from, to, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorDto("INVALID_DATE_RANGE", ex.Message, TraceId: HttpContext.TraceIdentifier));
        }
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);

    private UnauthorizedObjectResult UnauthorizedError() =>
        Unauthorized(new ApiErrorDto("UNAUTHENTICATED", "Geçerli oturum bulunamadı.", TraceId: HttpContext.TraceIdentifier));
}

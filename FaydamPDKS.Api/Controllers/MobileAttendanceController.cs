using FaydamPDKS.Core.DTOs.Attendance;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Api.Controllers;

[ApiController]
[Authorize(Roles = "Personel")]
[Route("api/v1/attendance")]
[Produces("application/json")]
public sealed class MobileAttendanceController(IAttendanceService attendance, IAttendanceReportService reports, TimeProvider clock) : ControllerBase
{
    [HttpGet("today")]
    [ProducesResponseType<TodayAttendanceDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Today(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        return Ok(await attendance.GetTodayAsync(userId, cancellationToken));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        if (from == default || to == default || from > to || to > today || to.DayNumber - from.DayNumber + 1 > 90)
            return BadRequest(new ApiErrorDto("INVALID_DATE_RANGE", "Tarih aralığı en fazla 90 gün olmalı ve gelecek tarih içermemelidir.", TraceId: HttpContext.TraceIdentifier));
        try
        {
            var report = await reports.GetAsync(from, to, userId, cancellationToken);
            var fileName = $"puantajim-{from:yyyyMMdd}-{to:yyyyMMdd}";
            return format.ToLowerInvariant() switch
            {
                "csv" => File(AttendanceExportBuilder.Csv(report), "text/csv; charset=utf-8", fileName + ".csv"),
                "pdf" => File(AttendanceExportBuilder.Pdf(report), "application/pdf", fileName + ".pdf"),
                _ => BadRequest(new ApiErrorDto("INVALID_FORMAT", "Format csv veya pdf olmalıdır.", TraceId: HttpContext.TraceIdentifier))
            };
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorDto("INVALID_DATE_RANGE", ex.Message, TraceId: HttpContext.TraceIdentifier));
        }
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

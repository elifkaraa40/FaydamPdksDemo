using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FaydamPDKS.Api.Controllers;

[ApiController, AllowAnonymous]
[Route("api/v1/terminals")]
[Produces("application/json")]
public sealed class TerminalHeartbeatController(IAttendanceTerminalService terminals) : ControllerBase
{
    [HttpPost("{terminalId:guid}/heartbeat")]
    [EnableRateLimiting("terminal-heartbeat")]
    public async Task<IActionResult> Heartbeat(Guid terminalId, TerminalHeartbeatDto request, CancellationToken cancellationToken)
    {
        var apiKey = Request.Headers["X-Terminal-Key"].ToString();
        if (string.IsNullOrWhiteSpace(apiKey) || !await terminals.HeartbeatAsync(terminalId, apiKey, request, cancellationToken))
            return Unauthorized(new { code = "INVALID_TERMINAL_CREDENTIALS", message = "Terminal kimliği doğrulanamadı.", traceId = HttpContext.TraceIdentifier });
        return NoContent();
    }
}

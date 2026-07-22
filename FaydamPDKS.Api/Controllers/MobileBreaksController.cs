using System.Security.Claims;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Api.Controllers;

[ApiController, Authorize(Roles = "Personel")]
[Route("api/v1/breaks")]
public sealed class MobileBreaksController(IBreakService breaks) : ControllerBase
{
    [HttpGet("current")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken) =>
        TryUserId(out var id) ? Ok(await breaks.GetCurrentAsync(id, cancellationToken)) : UnauthorizedError();

    [HttpPost("start")]
    public async Task<IActionResult> Start(StartBreakRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(id => breaks.StartAsync(id, request.DeviceEventId, cancellationToken));

    [HttpPost("{breakId:guid}/end")]
    public async Task<IActionResult> End(Guid breakId, EndBreakRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(id => breaks.EndAsync(id, breakId, request.DeviceEventId, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> History([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken) =>
        TryUserId(out var id) ? Ok(await breaks.GetHistoryAsync(id, from, to, cancellationToken)) : UnauthorizedError();

    [HttpGet("active-colleagues"), Authorize(Roles = "Personel")]
    public async Task<IActionResult> ActiveColleagues(CancellationToken cancellationToken) =>
        TryUserId(out var id) ? Ok(await breaks.GetActiveColleaguesAsync(id, cancellationToken)) : UnauthorizedError();

    private async Task<IActionResult> ExecuteAsync(Func<Guid, Task<CurrentBreakDto>> action)
    {
        if (!TryUserId(out var id)) return UnauthorizedError();
        try { return Ok(await action(id)); }
        catch (InvalidOperationException ex) { return Conflict(Error(ex.Message, BreakMessage(ex.Message))); }
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private UnauthorizedObjectResult UnauthorizedError() => Unauthorized(Error("UNAUTHENTICATED", "Geçerli oturum bulunamadı."));
    private ApiErrorDto Error(string code, string message) => new(code, message, TraceId: HttpContext.TraceIdentifier);
    private static string BreakMessage(string code) => code switch
    {
        "BREAK_ALREADY_ACTIVE" => "Zaten devam eden bir molanız var.",
        "BREAK_REQUIRES_ACTIVE_ATTENDANCE" => "Mola başlatmak için aktif bir giriş kaydınız olmalıdır.",
        "ACTIVE_BREAK_NOT_FOUND" => "Aktif mola bulunamadı.",
        "DUPLICATE_EVENT" => "Bu cihaz olayı daha önce işlendi.",
        "ACTIVE_USER_NOT_FOUND" => "Aktif kullanıcı bulunamadı.",
        _ => "Mola işlemi tamamlanamadı."
    };
}

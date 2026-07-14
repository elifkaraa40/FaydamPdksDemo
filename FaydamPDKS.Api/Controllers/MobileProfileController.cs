using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Api.Controllers;

[ApiController, Authorize]
[Route("api/v1/me")]
[Produces("application/json")]
public sealed class MobileProfileController(IMobileProfileService profiles, IPersonalDataExportService personalData) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        var profile = await profiles.GetAsync(userId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateMobileProfileDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        var profile = await profiles.UpdateAsync(userId, request, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("export")]
    [Produces("application/json")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return UnauthorizedError();
        var export = await personalData.ExportAsync(userId, cancellationToken);
        if (export is null) return NotFound();
        Response.Headers.ContentDisposition = $"attachment; filename=personal-data-{export.GeneratedAt:yyyyMMdd}.json";
        return Ok(export);
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    private UnauthorizedObjectResult UnauthorizedError() => Unauthorized(new ApiErrorDto("UNAUTHENTICATED", "Geçerli oturum bulunamadı.", TraceId: HttpContext.TraceIdentifier));
}

using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace FaydamPDKS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class MobileAuthController(IMobileAuthService auth, EmailRegistrationService registrations) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("mobile-auth")]
    [ProducesResponseType<MobileAuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(EmailRegistrationRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await registrations.RegisterAsync(request, cancellationToken)); }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_ALREADY_REGISTERED")
        {
            return Conflict(Error("EMAIL_ALREADY_REGISTERED", "Bu e-posta adresiyle daha önce kayıt oluşturulmuş. Giriş ekranını kullanın."));
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("mobile-auth")]
    [ProducesResponseType<MobileAuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorDto>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(MobileLoginRequest request, CancellationToken cancellationToken)
    {
        var response = await auth.LoginAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(Error("INVALID_CREDENTIALS", "E-posta veya parola hatalı."))
            : Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("mobile-auth")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await auth.RefreshAsync(request.RefreshToken, cancellationToken);
        return response is null
            ? Unauthorized(Error("INVALID_REFRESH_TOKEN", "Oturum yenilenemedi; tekrar giriş yapın."))
            : Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId))
            return Unauthorized(Error("UNAUTHENTICATED", "Geçerli oturum bulunamadı."));
        await auth.RevokeAsync(userId, request.RefreshToken, cancellationToken);
        return NoContent();
    }

    private ApiErrorDto Error(string code, string message) => new(code, message, TraceId: HttpContext.TraceIdentifier);
}

public sealed record RefreshRequest(string RefreshToken);

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Core.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult Success(object? data = null, string message = "İşlem başarılı.") =>
        Ok(new { success = true, message, data });

    protected IActionResult Fail(string message, int statusCode = StatusCodes.Status400BadRequest) =>
        StatusCode(statusCode, new { success = false, message, data = (object?)null });

    protected Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    protected string? GetCurrentUserRole() => User.FindFirstValue(ClaimTypes.Role);
}

using System.Security.Claims;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class PasswordResetRequestsController(IPasswordResetService passwordResets) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var managerId)) return Challenge();
        return View(await passwordResets.GetPendingManagerRequestsAsync(managerId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Review(Guid id, bool approve, string? note, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var managerId)) return Challenge();
        try
        {
            var result = await passwordResets.ReviewManagerRequestAsync(
                id, managerId, approve, note, HttpContext.TraceIdentifier, cancellationToken);
            if (!result.Found) return NotFound();
            if (approve)
            {
                TempData["TemporaryPassword"] = result.TemporaryPassword;
                TempData["Success"] = "Şifre sıfırlandı. Geçici şifre yalnızca bu ekranda bir kez gösterilir.";
            }
            else TempData["Success"] = "Şifre sıfırlama talebi reddedildi.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or UnauthorizedAccessException)
        {
            TempData["Error"] = exception.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}

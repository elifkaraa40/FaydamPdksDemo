using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class AttendanceCorrectionsController(IWebAttendanceCorrectionService corrections) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View("~/Views/Home/AttendanceCorrections.cshtml", await corrections.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Review(Guid id, ReviewAttendanceCorrectionDto request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var reviewerId)) return Challenge();
        try
        {
            if (!await corrections.ReviewAsync(id, reviewerId, request, HttpContext.TraceIdentifier, cancellationToken)) return NotFound();
            TempData["Success"] = request.Approve ? "Düzeltme talebi onaylandı." : "Düzeltme talebi reddedildi.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}

using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class ShiftsController(IShiftAdminService shifts) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View("~/Views/Home/Shifts.cshtml", await shifts.GetPageAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateShiftDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) TempData["Error"] = "Vardiya alanlarını kontrol edin.";
        else try { await shifts.CreateShiftAsync(request, cancellationToken); TempData["Success"] = "Vardiya oluşturuldu."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Assign(CreateShiftAssignmentDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) TempData["Error"] = "Atama alanlarını kontrol edin.";
        else try { await shifts.AssignAsync(request, cancellationToken); TempData["Success"] = "Vardiya personele atandı."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}

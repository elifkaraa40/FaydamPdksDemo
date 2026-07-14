using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class WorkCalendarController(IWorkCalendarAdminService calendar) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View("~/Views/Home/WorkCalendar.cshtml", await calendar.GetPageAsync(cancellationToken));
    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkCalendarDayDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) TempData["Error"] = "Takvim alanlarını kontrol edin.";
        else try { await calendar.CreateAsync(request, cancellationToken); TempData["Success"] = "Çalışma takvimi kaydı oluşturuldu."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}

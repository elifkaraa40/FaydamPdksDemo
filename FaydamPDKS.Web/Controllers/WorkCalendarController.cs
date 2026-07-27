using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class WorkCalendarController(IWorkCalendarAdminService calendar) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int? year,
        int? month,
        string? view,
        Guid? workplaceId,
        CancellationToken cancellationToken) =>
        View("~/Views/Home/WorkCalendar.cshtml", await calendar.GetPageAsync(year, month, view, workplaceId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateWorkCalendarDayDto request,
        int? calendarYear,
        int? calendarMonth,
        string? calendarView,
        Guid? selectedWorkplaceId,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) TempData["Error"] = "Takvim alanlarını kontrol edin.";
        else try { await calendar.CreateAsync(request, cancellationToken); TempData["Success"] = "Çalışma takvimi kuralı kaydedildi."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new
        {
            year = calendarYear,
            month = calendarMonth,
            view = calendarView,
            workplaceId = selectedWorkplaceId
        });
    }
}

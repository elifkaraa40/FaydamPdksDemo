using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class EmployeesController(IEmployeeAdminService employees) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View("~/Views/Home/Employees.cshtml", await employees.GetPageAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Personel bilgilerini kontrol edin. Geçici parola en az 6 karakter olmalıdır.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            if (!TryActor(out var actorId)) return Challenge();
            await employees.CreateAsync(request, actorId, HttpContext.TraceIdentifier, cancellationToken);
            TempData["Success"] = "Personel kaydı oluşturuldu.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var employee = await employees.GetForEditAsync(id, cancellationToken);
        if (employee is null) return NotFound();
        var page = await employees.GetPageAsync(cancellationToken);
        ViewBag.Roles = page.Roles;
        ViewBag.Departments = page.Departments;
        return View("~/Views/Home/EditEmployee.cshtml", employee);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateEmployeeDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var page = await employees.GetPageAsync(cancellationToken);
            ViewBag.Roles = page.Roles;
            ViewBag.Departments = page.Departments;
            return View("~/Views/Home/EditEmployee.cshtml", request);
        }
        try
        {
            if (!TryActor(out var actorId)) return Challenge();
            if (!await employees.UpdateAsync(request, actorId, HttpContext.TraceIdentifier, cancellationToken)) return NotFound();
            TempData["Success"] = "Personel bilgileri güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var page = await employees.GetPageAsync(cancellationToken);
            ViewBag.Roles = page.Roles;
            ViewBag.Departments = page.Departments;
            return View("~/Views/Home/EditEmployee.cshtml", request);
        }
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(Guid id, bool active, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Challenge();
        try
        {
            if (!await employees.SetActiveAsync(id, active, actorId, HttpContext.TraceIdentifier, cancellationToken)) return NotFound();
            TempData["Success"] = active ? "Personel hesabı aktifleştirildi." : "Personel hesabı pasife alındı.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private bool TryActor(out Guid actorId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
}

using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class OrganizationController(IOrganizationAdminService organization) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View("~/Views/Home/Organization.cshtml", await organization.GetPageAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreateWorkplace(CreateWorkplaceDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) TempData["Error"] = "İşyeri alanlarını kontrol edin.";
        else try { await organization.CreateWorkplaceAsync(request, cancellationToken); TempData["Success"] = "İşyeri oluşturuldu."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateDepartment(CreateDepartmentDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) TempData["Error"] = "Bölüm alanlarını kontrol edin.";
        else try { await organization.CreateDepartmentAsync(request, cancellationToken); TempData["Success"] = "Bölüm oluşturuldu."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}

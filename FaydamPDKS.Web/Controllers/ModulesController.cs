using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class ModulesController(IDashboardQueryService dashboard) : Controller
{
    [HttpGet]
    public IActionResult Pdks() => View("~/Views/Home/PdksCenter.cshtml");

    [HttpGet]
    public IActionResult Reporting() => View("~/Views/Home/ReportingCenter.cshtml");

    [HttpGet]
    public async Task<IActionResult> Operations(CancellationToken cancellationToken) =>
        View("~/Views/Home/OperationsCenter.cshtml", await dashboard.GetAsync(cancellationToken));
}

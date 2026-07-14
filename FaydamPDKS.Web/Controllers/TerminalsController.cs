using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class TerminalsController(IAttendanceTerminalService terminals) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View("~/Views/Home/Terminals.cshtml", await terminals.GetPageAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Register(RegisterTerminalDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Terminal alanlarını kontrol edin."; return RedirectToAction(nameof(Index)); }
        try
        {
            var registered = await terminals.RegisterAsync(request, cancellationToken);
            ViewBag.NewTerminalId = registered.Id;
            ViewBag.NewTerminalApiKey = registered.ApiKey;
            return View("~/Views/Home/Terminals.cshtml", await terminals.GetPageAsync(cancellationToken));
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Index)); }
    }

    [HttpPost]
    public async Task<IActionResult> RotateKey(Guid id, CancellationToken cancellationToken)
    {
        var registered = await terminals.RotateKeyAsync(id, cancellationToken);
        if (registered is null) return NotFound();
        ViewBag.NewTerminalId = registered.Id;
        ViewBag.NewTerminalApiKey = registered.ApiKey;
        ViewBag.KeyRotated = true;
        return View("~/Views/Home/Terminals.cshtml", await terminals.GetPageAsync(cancellationToken));
    }
}

using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class AttendanceQrController(IAttendanceQrService qrCodes) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View("~/Views/Home/AttendanceQr.cshtml", await qrCodes.GetPageAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateAttendanceQrDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "QR alanlarını kontrol edin."; return RedirectToAction(nameof(Index)); }
        try
        {
            var generated = await qrCodes.CreateAsync(request, cancellationToken);
            return await GeneratedViewAsync(generated, false, cancellationToken);
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Index)); }
    }

    [HttpPost]
    public async Task<IActionResult> Rotate(Guid id, CancellationToken cancellationToken)
    {
        var generated = await qrCodes.RotateAsync(id, cancellationToken);
        return generated is null ? NotFound() : await GeneratedViewAsync(generated, true, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        if (!await qrCodes.DeactivateAsync(id, cancellationToken)) return NotFound();
        TempData["Success"] = "QR kod kullanım dışı bırakıldı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> GeneratedViewAsync(GeneratedAttendanceQrDto generated, bool rotated, CancellationToken cancellationToken)
    {
        using var data = QRCodeGenerator.GenerateQrCode(generated.RawValue, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(12, [16, 42, 160], [255, 255, 255], true);
        ViewBag.GeneratedQr = generated;
        ViewBag.GeneratedQrImage = $"data:image/png;base64,{Convert.ToBase64String(png)}";
        ViewBag.QrRotated = rotated;
        return View("~/Views/Home/AttendanceQr.cshtml", await qrCodes.GetPageAsync(cancellationToken));
    }
}

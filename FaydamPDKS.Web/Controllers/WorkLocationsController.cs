using System.Security.Claims;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FaydamPDKS.Web.Controllers;
[Authorize(Roles = "Yonetici")]
public sealed class WorkLocationsController(IWorkLocationService service) : Controller
{
 [HttpGet] public async Task<IActionResult> Index(CancellationToken ct)=>View(await service.GetManagementPageAsync(ct));
 [HttpPost] public async Task<IActionResult> Create(CreateWorkLocationAssignmentDto r,CancellationToken ct){if(!UserId(out var actor))return Challenge();try{await service.CreateAssignmentAsync(r,actor,ct);TempData["Success"]="Çalışma konumu planı oluşturuldu.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Index));}
 [HttpPost] public async Task<IActionResult> End(Guid id,CancellationToken ct){if(!UserId(out var actor))return Challenge();if(!await service.EndAssignmentAsync(id,actor,ct))return NotFound();TempData["Success"]="Plan sonlandırıldı.";return RedirectToAction(nameof(Index));}
 [HttpPost] public async Task<IActionResult> Review(Guid id,bool approve,string? note,CancellationToken ct){if(!UserId(out var actor))return Challenge();try{if(!await service.ReviewFieldRequestAsync(id,actor,approve,note,ct))return NotFound();TempData["Success"]=approve?"Saha görevi onaylandı.":"Saha görevi reddedildi.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Index));}
 private bool UserId(out Guid id)=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out id);
}

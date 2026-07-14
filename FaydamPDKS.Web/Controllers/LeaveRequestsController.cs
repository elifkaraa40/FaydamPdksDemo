using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class LeaveRequestsController(IWebLeaveApprovalService leaveApprovals) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View("~/Views/Home/LeaveRequests.cshtml", await leaveApprovals.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Review(Guid id, ReviewLeaveRequestDto request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var reviewerId)) return Challenge();
        try
        {
            if (!await leaveApprovals.ReviewAsync(id, reviewerId, request, cancellationToken)) return NotFound();
            TempData["Success"] = request.Approve ? "İzin talebi onaylandı." : "İzin talebi reddedildi.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}

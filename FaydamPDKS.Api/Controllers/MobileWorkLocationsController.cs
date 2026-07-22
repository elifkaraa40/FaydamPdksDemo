using System.Security.Claims;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FaydamPDKS.Api.Controllers;
[ApiController,Authorize(Roles = "Personel"),Route("api/v1/work-locations")]
public sealed class MobileWorkLocationsController(IWorkLocationService service,TimeProvider clock):ControllerBase
{
 [HttpGet("today")] public async Task<IActionResult> Today(CancellationToken ct){if(!UserId(out var id))return Unauthorized();var date=DateOnly.FromDateTime(clock.GetLocalNow().DateTime);var x=await service.GetForDateAsync(id,date,ct);return Ok(x is null?new{date,workLocation="Office",recordSource="QR"}:new{date,workLocation=x.LocationType.ToString(),recordSource="WorkLocationPlan",x.ProjectName,x.CustomerName,x.FieldAddress,x.Reason});}
 [HttpGet("requests"), HttpGet("field-requests")] public async Task<IActionResult> Mine(CancellationToken ct)=>UserId(out var id)?Ok(await service.GetMyRequestsAsync(id,ct)):Unauthorized();
 [HttpPost("requests"), HttpPost("field-requests")] public async Task<IActionResult> Create(CreateFieldWorkRequestDto r,CancellationToken ct){if(!UserId(out var id))return Unauthorized();try{await service.CreateFieldRequestAsync(id,r,ct);return Accepted();}catch(InvalidOperationException ex){return Conflict(new FaydamPDKS.Core.DTOs.Common.ApiErrorDto("WORK_LOCATION_REQUEST_REJECTED",ex.Message,TraceId:HttpContext.TraceIdentifier));}}
 [HttpDelete("requests/{id:guid}"), HttpDelete("field-requests/{id:guid}")] public async Task<IActionResult> Cancel(Guid id,CancellationToken ct)=>UserId(out var uid)&&await service.CancelFieldRequestAsync(id,uid,ct)?NoContent():NotFound(new FaydamPDKS.Core.DTOs.Common.ApiErrorDto("PENDING_REQUEST_NOT_FOUND","Yalnızca bekleyen kendi talebiniz iptal edilebilir.",TraceId:HttpContext.TraceIdentifier));
 private bool UserId(out Guid id)=>Guid.TryParse(User.FindFirstValue("sub")??User.FindFirstValue(ClaimTypes.NameIdentifier),out id);
}

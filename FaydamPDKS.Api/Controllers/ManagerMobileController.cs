using System.Security.Claims;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.DTOs.Common;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Api.Controllers;

[ApiController]
[Authorize(Roles = "Yonetici")]
[Route("api/v1/manager")]
public sealed class ManagerMobileController(IManagerMobileService manager) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken ct) => ExecuteAsync(id => manager.GetDashboardAsync(id, ct));

    [HttpGet("approvals/summary")]
    public Task<IActionResult> Summary(CancellationToken ct) => ExecuteAsync(id => manager.GetApprovalsSummaryAsync(id, ct));

    [HttpGet("registrations")]
    public Task<IActionResult> Registrations([FromQuery] string? status = "Pending", [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        ExecuteAsync(id => manager.GetRegistrationsAsync(id, RegistrationStatus(status), page, pageSize, ct));

    [HttpPost("registrations/{id:guid}/review")]
    public Task<IActionResult> ReviewRegistration(Guid id, ReviewRegistrationDto request, CancellationToken ct) =>
        ReviewAsync(uid => manager.ReviewRegistrationAsync(id, uid, request, HttpContext.TraceIdentifier, ct));

    [HttpGet("leave-requests")]
    public Task<IActionResult> LeaveRequests([FromQuery] LeaveRequestStatus? status = LeaveRequestStatus.Pending, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        ExecuteAsync(id => manager.GetLeaveRequestsAsync(id, status, page, pageSize, ct));

    [HttpPost("leave-requests/{id:guid}/review")]
    public Task<IActionResult> ReviewLeave(Guid id, ReviewDecisionDto request, CancellationToken ct) =>
        ReviewAsync(uid => manager.ReviewLeaveRequestAsync(id, uid, new(request.Approve, request.Note), HttpContext.TraceIdentifier, ct));

    [HttpGet("attendance-corrections")]
    public Task<IActionResult> Corrections([FromQuery] AttendanceCorrectionStatus? status = AttendanceCorrectionStatus.Pending, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        ExecuteAsync(id => manager.GetAttendanceCorrectionsAsync(id, status, page, pageSize, ct));

    [HttpPost("attendance-corrections/{id:guid}/review")]
    public Task<IActionResult> ReviewCorrection(Guid id, ReviewDecisionDto request, CancellationToken ct) =>
        ReviewAsync(uid => manager.ReviewAttendanceCorrectionAsync(id, uid, new(request.Approve, request.Note), HttpContext.TraceIdentifier, ct));

    [HttpGet("work-location-requests")]
    public Task<IActionResult> WorkLocations([FromQuery] WorkLocationRequestStatus? status = WorkLocationRequestStatus.Pending, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        ExecuteAsync(id => manager.GetWorkLocationRequestsAsync(id, status, page, pageSize, ct));

    [HttpPost("work-location-requests/{id:guid}/review")]
    public Task<IActionResult> ReviewWorkLocation(Guid id, ReviewDecisionDto request, CancellationToken ct) =>
        ReviewAsync(uid => manager.ReviewWorkLocationRequestAsync(id, uid, request.Approve, request.Note, HttpContext.TraceIdentifier, ct));

    [HttpGet("personnel-status")]
    public Task<IActionResult> PersonnelStatus([FromQuery] Guid? workplaceId, [FromQuery] Guid? departmentId, [FromQuery] string? status,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        ExecuteAsync(id => manager.GetPersonnelStatusAsync(id, workplaceId, departmentId, status, search, page, pageSize, ct));

    [HttpGet("attendance-report")]
    public Task<IActionResult> AttendanceReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? workplaceId,
        [FromQuery] Guid? departmentId, [FromQuery] Guid? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        ExecuteAsync(id => manager.GetAttendanceReportAsync(id, from, to, workplaceId, departmentId, userId, page, pageSize, ct));

    [HttpGet("attendance-report/export")]
    public async Task<IActionResult> Export([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? workplaceId,
        [FromQuery] Guid? departmentId, [FromQuery] Guid? userId, [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        if (!TryUserId(out var id)) return UnauthorizedError();
        try
        {
            var report = await manager.GetAttendanceReportExportAsync(id, from, to, workplaceId, departmentId, userId, HttpContext.TraceIdentifier, ct);
            var safeName = $"puantaj-{from:yyyyMMdd}-{to:yyyyMMdd}";
            return format.ToLowerInvariant() switch
            {
                "csv" => File(AttendanceExportBuilder.Csv(report), "text/csv; charset=utf-8", safeName + ".csv"),
                "pdf" => File(AttendanceExportBuilder.Pdf(report), "application/pdf", safeName + ".pdf"),
                "xlsx" => File(AttendanceExportBuilder.Xlsx(report), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", safeName + ".xlsx"),
                _ => BadRequest(Error("INVALID_FORMAT", "Format csv, pdf veya xlsx olmalıdır."))
            };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { return Failure(ex); }
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Guid, Task<T>> action)
    {
        if (!TryUserId(out var id)) return UnauthorizedError();
        try { return Ok(await action(id)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { return Failure(ex); }
    }
    private async Task<IActionResult> ReviewAsync(Func<Guid, Task<bool>> action)
    {
        if (!TryUserId(out var id)) return UnauthorizedError();
        try { return await action(id) ? Ok(new { status = "Reviewed" }) : NotFound(Error("NOT_FOUND", "Talep bulunamadı.")); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { return Failure(ex); }
    }
    private IActionResult Failure(Exception ex) => ex is UnauthorizedAccessException
        ? StatusCode(StatusCodes.Status403Forbidden, Error("FORBIDDEN_SCOPE", ex.Message))
        : Conflict(Error("REQUEST_CONFLICT", ex.Message));
    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private UnauthorizedObjectResult UnauthorizedError() => Unauthorized(Error("UNAUTHENTICATED", "Geçerli oturum bulunamadı."));
    private ApiErrorDto Error(string code, string message) => new(code, message, TraceId: HttpContext.TraceIdentifier);
    private static AccountStatus? RegistrationStatus(string? value) => string.Equals(value, "Pending", StringComparison.OrdinalIgnoreCase)
        ? AccountStatus.PendingApproval : Enum.TryParse<AccountStatus>(value, true, out var parsed) ? parsed : null;
}

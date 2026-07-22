using System.Globalization;
using System.Security.Claims;
using System.Text;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaydamPDKS.Web.Controllers;

[Authorize]
public sealed class MyWorkController(
    IAttendanceReportService attendanceReports,
    ILeaveRequestRepository leaveRequests,
    IBreakService breaks,
    IWorkCalendarResolver workCalendar,
    IAttendanceCorrectionRepository corrections,
    IManagerNotificationService managerNotifications,
    IWorkLocationService workLocations,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : Controller
{
    [HttpGet]
    public async Task<IActionResult> FieldWork(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        return View(await workLocations.GetMyRequestsAsync(userId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateFieldWork(CreateFieldWorkRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        try { await workLocations.CreateFieldRequestAsync(userId, request, cancellationToken); TempData["Success"] = "Çalışma konumu talebiniz yöneticiye gönderildi."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(FieldWork));
    }

    [HttpPost]
    public async Task<IActionResult> CancelFieldWork(Guid id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        if (!await workLocations.CancelFieldRequestAsync(id, userId, cancellationToken)) return NotFound();
        TempData["Success"] = "Saha görevi talebi iptal edildi.";
        return RedirectToAction(nameof(FieldWork));
    }
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var range = ResolveRange(from, to);
        return View(await BuildModelAsync(userId, range.From, range.To, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Records(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var range = ResolveRange(from, to);
        return View(await BuildModelAsync(userId, range.From, range.To, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Leaves(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var leaves = await leaveRequests.GetForUserAsync(userId, cancellationToken);
        return View(new MyWorkViewModel(today, today, [], [], leaves)
        {
            LeaveWorkDayCounts = await LeaveWorkDayCountsAsync(userId, leaves, cancellationToken)
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var range = ResolveRange(from, to);
        var rows = (await GetMyReportAsync(userId, range.From, range.To, cancellationToken)).Rows;
        var csv = new StringBuilder("Tarih;Vardiya;Durum;İlk Giriş;Son Çıkış;Çalışılan Dakika;Beklenen Dakika;Geç Dakika;Fazla Mesai Dakika\r\n");
        foreach (var row in rows)
            csv.AppendJoin(';', row.WorkDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), Csv(row.ShiftName), Csv(StatusLabel(row.Status)),
                Time(row.FirstEntry), Time(row.LastExit), row.WorkedMinutes, row.ExpectedMinutes, row.LateMinutes, row.OvertimeMinutes).Append("\r\n");
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8",
            $"puantajim-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var range = ResolveRange(from, to);
        var report = await GetMyReportAsync(userId, range.From, range.To, cancellationToken);
        return File(AttendanceReportFileBuilder.Excel(report),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"puantajim-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var range = ResolveRange(from, to);
        var report = await GetMyReportAsync(userId, range.From, range.To, cancellationToken);
        return File(AttendanceReportFileBuilder.Pdf(report), "application/pdf",
            $"puantajim-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.pdf");
    }

    [HttpPost]
    public async Task<IActionResult> CreateLeave(LeaveType leaveType, LeaveDayPortion dayPortion, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (startDate < today || startDate > endDate || endDate.DayNumber - startDate.DayNumber >= 365)
        {
            TempData["Error"] = "İzin tarihlerini kontrol edin; geçmiş tarih seçilemez.";
            return RedirectToAction(nameof(Leaves));
        }
        if (dayPortion != LeaveDayPortion.FullDay && startDate != endDate)
        {
            TempData["Error"] = "Yarım gün izin için başlangıç ve bitiş aynı tarih olmalıdır.";
            return RedirectToAction(nameof(Leaves));
        }
        if (await leaveRequests.HasActiveOverlapAsync(userId, startDate, endDate, cancellationToken))
        {
            TempData["Error"] = "Seçilen tarihlerde bekleyen veya onaylanmış başka bir izin talebiniz var.";
            return RedirectToAction(nameof(Leaves));
        }
        var leave = new LeaveRequest
        {
            UserId = userId, LeaveType = leaveType, DayPortion = dayPortion, StartDate = startDate, EndDate = endDate,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Status = LeaveRequestStatus.Pending, CreatedAt = timeProvider.GetUtcNow()
        };
        await leaveRequests.AddAsync(leave, cancellationToken);
        await managerNotifications.NotifyAsync(NotificationType.LeaveRequestCreated, "Yeni izin talebi",
            $"{User.Identity?.Name} {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy} tarihleri için izin talep etti.", leave.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "İzin talebiniz yönetici onayına gönderildi.";
        return RedirectToAction(nameof(Leaves));
    }

    [HttpPost]
    public async Task<IActionResult> CancelLeave(Guid id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var entity = await leaveRequests.GetForUserByIdAsync(userId, id, cancellationToken);
        if (entity is null) return NotFound();
        if (entity.Status != LeaveRequestStatus.Pending)
        {
            TempData["Error"] = "Yalnızca bekleyen izin talepleri iptal edilebilir.";
            return RedirectToAction(nameof(Leaves));
        }
        entity.Status = LeaveRequestStatus.Cancelled;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "İzin talebiniz iptal edildi.";
        return RedirectToAction(nameof(Leaves));
    }

    [HttpPost]
    public async Task<IActionResult> CreateCorrection(CreateAttendanceCorrectionDto request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId)) return Challenge();
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (request.WorkDate > today || request.WorkDate < today.AddDays(-90) ||
            (request.CorrectionType == AttendanceCorrectionType.TimeCorrection && request.RequestedEntry == request.RequestedExit) || request.Reason.Trim().Length < 10)
        {
            TempData["Error"] = "Düzeltme tarihi, saatleri veya en az 10 karakterlik gerekçeyi kontrol edin.";
            return RedirectToAction(nameof(Records));
        }
        if (await corrections.HasPendingAsync(userId, request.WorkDate, cancellationToken))
        {
            TempData["Error"] = "Bu tarih için zaten bekleyen bir düzeltme talebiniz var.";
            return RedirectToAction(nameof(Records));
        }
        var correction = new AttendanceCorrectionRequest
        {
            Id = Guid.NewGuid(), UserId = userId, WorkDate = request.WorkDate,
            CorrectionType = request.CorrectionType,
            RequestedEntry = request.RequestedEntry, RequestedExit = request.RequestedExit,
            ProjectName = request.ProjectName?.Trim(), CustomerName = request.CustomerName?.Trim(), FieldAddress = request.FieldAddress?.Trim(),
            Reason = request.Reason.Trim(), Status = AttendanceCorrectionStatus.Pending,
            CreatedAt = timeProvider.GetUtcNow()
        };
        await corrections.AddAsync(correction, cancellationToken);
        await managerNotifications.NotifyAsync(NotificationType.AttendanceCorrectionCreated, "Yeni puantaj düzeltme talebi",
            $"{User.Identity?.Name} {request.WorkDate:dd.MM.yyyy} tarihli puantaj için düzeltme talep etti.", correction.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Puantaj düzeltme talebiniz yönetici onayına gönderildi.";
        return RedirectToAction(nameof(Records));
    }

    private async Task<AttendanceReportDto> GetMyReportAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var report = await attendanceReports.GetAsync(from, to, cancellationToken: cancellationToken);
        return new AttendanceReportDto(from, to, report.Rows.Where(x => x.EmployeeId == userId).ToArray());
    }

    private async Task<MyWorkViewModel> BuildModelAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var attendance = (await attendanceReports.GetAsync(from, to, cancellationToken: cancellationToken)).Rows
            .Where(x => x.EmployeeId == userId).ToArray();
        var breakHistory = await breaks.GetHistoryAsync(userId, from, to, cancellationToken);
        var leaves = await leaveRequests.GetForUserAsync(userId, cancellationToken);
        return new(from, to, attendance, breakHistory, leaves, await corrections.GetForUserAsync(userId, cancellationToken))
        {
            LeaveWorkDayCounts = await LeaveWorkDayCountsAsync(userId, leaves, cancellationToken)
        };
    }

    private async Task<IReadOnlyDictionary<Guid, double>> LeaveWorkDayCountsAsync(Guid userId, IReadOnlyList<LeaveRequest> leaves, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, double>();
        foreach (var leave in leaves)
        {
            var count = 0d;
            for (var date = leave.StartDate; date <= leave.EndDate; date = date.AddDays(1))
                if ((await workCalendar.ResolveAsync(userId, date, cancellationToken)).IsWorkingDay) count++;
            result[leave.Id] = leave.DayPortion == LeaveDayPortion.FullDay ? count : Math.Min(.5, count);
        }
        return result;
    }

    private (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var end = to ?? today;
        var start = from ?? new DateOnly(end.Year, end.Month, 1);
        if (start > end || end.DayNumber - start.DayNumber >= 31) throw new ArgumentException("En fazla 31 günlük geçerli bir aralık seçin.");
        return (start, end);
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private static string Time(DateTimeOffset? value) => value?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static string StatusLabel(string status) => status switch
    {
        "Complete" => "Tamamlandı", "NoRecord" => "Kayıt yok", "NonWorkingDay" => "Çalışma dışı gün",
        "MissingEntry" => "Giriş eksik", "MissingExit" => "Çıkış eksik", _ => status
    };
}

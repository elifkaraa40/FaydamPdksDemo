using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class ReportsController(
    IAttendanceReportService reports,
    WebReportingService webReports,
    TimeProvider timeProvider) : Controller
{
    [HttpGet]
    public IActionResult Index(DateOnly? from, DateOnly? to, Guid? employeeId) =>
        RedirectToAction(nameof(Attendance), new { from, to, employeeId });

    [HttpGet]
    public async Task<IActionResult> Attendance(DateOnly? from, DateOnly? to, Guid? employeeId, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try { return View("~/Views/Home/Reports.cshtml", await reports.GetAsync(range.From, range.To, employeeId, cancellationToken)); }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("~/Views/Home/Reports.cshtml", new FaydamPDKS.Core.DTOs.AttendanceReportDto(range.From, range.To, [])); }
    }

    [HttpGet]
    public async Task<IActionResult> Transitions(DateOnly? from, DateOnly? to, Guid? employeeId,
        string? eventType, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try
        {
            return View("~/Views/Home/TransitionReport.cshtml",
                await webReports.GetTransitionsAsync(range.From, range.To, employeeId, eventType, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("~/Views/Home/TransitionReport.cshtml",
                new TransitionReportViewModel(range.From, range.To, employeeId, eventType, [], []));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Leaves(DateOnly? from, DateOnly? to, Guid? employeeId,
        LeaveType? leaveType, LeaveRequestStatus? status, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try
        {
            return View("~/Views/Home/LeaveReport.cshtml",
                await webReports.GetLeavesAsync(range.From, range.To, employeeId, leaveType, status, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("~/Views/Home/LeaveReport.cshtml",
                new LeaveReportViewModel(range.From, range.To, employeeId, leaveType, status, [], []));
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(DateOnly? from, DateOnly? to, Guid? employeeId, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        FaydamPDKS.Core.DTOs.AttendanceReportDto report;
        try { report = await reports.GetAsync(range.From, range.To, employeeId, cancellationToken); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        var csv = new StringBuilder("Sicil No;Personel;Bölüm;Tarih;Vardiya;Durum;Çalışma Şekli;Çalışma Detayı;İlk Giriş;Son Çıkış;Çalışılan Dakika;Beklenen Dakika;Geç Dakika;Fazla Mesai Dakika\r\n");
        foreach (var row in report.Rows)
        {
            csv.AppendJoin(';', Csv(row.EmployeeNumber), Csv(row.EmployeeName), Csv(row.Department),
                row.WorkDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), Csv(row.ShiftName), Csv(StatusLabel(row.Status)), Csv(WorkLocationLabel(row.WorkLocation)), Csv(row.WorkLocationDetail),
                Time(row.FirstEntry), Time(row.LastExit), row.WorkedMinutes, row.ExpectedMinutes, row.LateMinutes, row.OvertimeMinutes).Append("\r\n");
        }
        var bytes = new UTF8Encoding(true).GetBytes(csv.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"puantaj-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to, Guid? employeeId, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try
        {
            var report = await reports.GetAsync(range.From, range.To, employeeId, cancellationToken);
            return File(AttendanceReportFileBuilder.Excel(report),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"puantaj-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.xlsx");
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? from, DateOnly? to, Guid? employeeId, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try
        {
            var report = await reports.GetAsync(range.From, range.To, employeeId, cancellationToken);
            return File(AttendanceReportFileBuilder.Pdf(report), "application/pdf",
                $"puantaj-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet]
    public async Task<IActionResult> ExportTransitionsCsv(DateOnly? from, DateOnly? to, Guid? employeeId,
        string? eventType, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        TransitionReportViewModel report;
        try { report = await webReports.GetTransitionsAsync(range.From, range.To, employeeId, eventType, cancellationToken); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        var csv = new StringBuilder("Tarih;Saat;Sicil No;Personel;Bölüm;İşlem;İşyeri;Bölge;Kaynak\r\n");
        foreach (var row in report.Rows)
            csv.AppendJoin(';', row.WorkDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                row.EventTime.ToString("HH:mm", CultureInfo.InvariantCulture), Csv(row.EmployeeNumber),
                Csv(row.EmployeeName), Csv(row.Department), Csv(EventTypeLabel(row.EventType)),
                Csv(row.WorkplaceName), Csv(row.ZoneName), Csv(row.Source)).Append("\r\n");
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8",
            $"qr-hareketleri-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportTransitionsExcel(DateOnly? from, DateOnly? to, Guid? employeeId,
        string? eventType, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try
        {
            var report = await webReports.GetTransitionsAsync(range.From, range.To, employeeId, eventType, cancellationToken);
            return File(ReportTableFileBuilder.TransitionsExcel(report),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"qr-hareketleri-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.xlsx");
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet]
    public async Task<IActionResult> ExportTransitionsPdf(DateOnly? from, DateOnly? to, Guid? employeeId,
        string? eventType, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try
        {
            var report = await webReports.GetTransitionsAsync(range.From, range.To, employeeId, eventType, cancellationToken);
            return File(ReportTableFileBuilder.TransitionsPdf(report), "application/pdf",
                $"qr-hareketleri-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeavesCsv(DateOnly? from, DateOnly? to, Guid? employeeId,
        LeaveType? leaveType, LeaveRequestStatus? status, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        LeaveReportViewModel report;
        try { report = await webReports.GetLeavesAsync(range.From, range.To, employeeId, leaveType, status, cancellationToken); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        var csv = new StringBuilder("Sicil No;Personel;Bölüm;Başlangıç;Bitiş;İzin Türü;Gün Türü;İş Günü;Durum;Açıklama\r\n");
        foreach (var row in report.Rows)
            csv.AppendJoin(';', Csv(row.EmployeeNumber), Csv(row.EmployeeName), Csv(row.Department),
                row.StartDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                row.EndDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                Csv(FaydamPDKS.Core.LeaveLabels.Type(row.LeaveType)),
                Csv(FaydamPDKS.Core.LeaveLabels.Portion(row.DayPortion)),
                row.WorkDayCount.ToString("0.#", CultureInfo.GetCultureInfo("tr-TR")),
                Csv(FaydamPDKS.Core.LeaveLabels.Status(row.Status)), Csv(row.Reason)).Append("\r\n");
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8",
            $"izin-raporu-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeavesExcel(DateOnly? from, DateOnly? to, Guid? employeeId,
        LeaveType? leaveType, LeaveRequestStatus? status, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try
        {
            var report = await webReports.GetLeavesAsync(range.From, range.To, employeeId, leaveType, status, cancellationToken);
            return File(ReportTableFileBuilder.LeavesExcel(report),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"izin-raporu-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.xlsx");
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeavesPdf(DateOnly? from, DateOnly? to, Guid? employeeId,
        LeaveType? leaveType, LeaveRequestStatus? status, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try
        {
            var report = await webReports.GetLeavesAsync(range.From, range.To, employeeId, leaveType, status, cancellationToken);
            return File(ReportTableFileBuilder.LeavesPdf(report), "application/pdf",
                $"izin-raporu-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    private (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var end = to ?? today;
        return (from ?? end.AddDays(-6), end);
    }

    private static string Time(DateTimeOffset? value) => value?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static string StatusLabel(string status) => status switch { "Complete" => "Tamamlandı", "NoRecord" => "Kayıt yok", "NonWorkingDay" => "Çalışma dışı gün", "MissingEntry" => "Giriş eksik", "MissingExit" => "Çıkış eksik", "RemoteWork" => "Uzaktan çalışma", "FieldWork" => "Saha çalışması", _ => status };
    private static string WorkLocationLabel(string location) => location switch { "Remote" => "Uzaktan", "Field" => "Saha", _ => "Ofis" };
    private static string EventTypeLabel(string eventType) => eventType switch { "Giris" => "Giriş", "Cikis" => "Çıkış", _ => eventType };
}

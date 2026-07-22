using FaydamPDKS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace FaydamPDKS.Web.Controllers;

[Authorize(Roles = "Yonetici")]
public sealed class ReportsController(IAttendanceReportService reports, TimeProvider timeProvider) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, Guid? employeeId, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        try { return View("~/Views/Home/Reports.cshtml", await reports.GetAsync(range.From, range.To, employeeId, cancellationToken)); }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("~/Views/Home/Reports.cshtml", new FaydamPDKS.Core.DTOs.AttendanceReportDto(range.From, range.To, [])); }
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

    private (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var end = to ?? today;
        return (from ?? end.AddDays(-6), end);
    }

    private static string Time(DateTimeOffset? value) => value?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static string StatusLabel(string status) => status switch { "Complete" => "Tamamlandı", "NoRecord" => "Kayıt yok", "NonWorkingDay" => "Çalışma dışı gün", "MissingEntry" => "Giriş eksik", "MissingExit" => "Çıkış eksik", "RemoteWork" => "Uzaktan çalışma", "FieldWork" => "Saha çalışması", _ => status };
    private static string WorkLocationLabel(string location) => location switch { "Remote" => "Uzaktan", "Field" => "Saha", _ => "Ofis" };
}

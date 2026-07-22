using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Web;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class AttendanceReportFileBuilderTests
{
    [Fact]
    public void Pdf_export_is_well_formed()
    {
        var employeeId = Guid.NewGuid();
        var rows = Enumerable.Range(0, 24).Select(index => new AttendanceReportRowDto(
            employeeId, "PER-0001", "Demo Personel", "Üretim Bölümü", new DateOnly(2026, 7, 14).AddDays(index),
            "Varsayılan vardiya", index % 3 == 0 ? "NoRecord" : "Complete", null, null,
            index % 3 == 0 ? 0 : 480, 480, 0, 0)).ToArray();
        var report = new AttendanceReportDto(new DateOnly(2026, 7, 14), new DateOnly(2026, 8, 6), rows);

        var pdf = AttendanceReportFileBuilder.Pdf(report);

        Assert.StartsWith("%PDF-1.4", System.Text.Encoding.ASCII.GetString(pdf, 0, 8));
        Assert.True(pdf.Length > 1000);
        var qaPath = Environment.GetEnvironmentVariable("PDF_QA_PATH");
        if (!string.IsNullOrWhiteSpace(qaPath)) File.WriteAllBytes(qaPath, pdf);
    }

    [Fact]
    public void Excel_export_contains_styles_filters_and_frozen_header()
    {
        var employeeId = Guid.NewGuid();
        var rows = Enumerable.Range(0, 12).Select(index => new AttendanceReportRowDto(
            employeeId, $"PER-{index + 1:0000}", $"Demo Personel {index + 1}", "Üretim Bölümü", new DateOnly(2026, 7, 14).AddDays(index),
            "Varsayılan vardiya", index % 3 == 0 ? "NoRecord" : "Complete", null, null,
            index % 3 == 0 ? 0 : 480, 480, index, index * 2)).ToArray();
        var report = new AttendanceReportDto(new DateOnly(2026, 7, 14), new DateOnly(2026, 7, 25), rows);

        var excel = AttendanceReportFileBuilder.Excel(report);

        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(excel));
        Assert.NotNull(archive.GetEntry("xl/styles.xml"));
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheetXml = reader.ReadToEnd();
        Assert.Contains("state=\"frozen\"", sheetXml);
        Assert.Contains("autoFilter", sheetXml);
        var qaPath = Environment.GetEnvironmentVariable("EXCEL_QA_PATH");
        if (!string.IsNullOrWhiteSpace(qaPath)) File.WriteAllBytes(qaPath, excel);
    }
}

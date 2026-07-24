using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using FaydamPDKS.Api;
using FaydamPDKS.Core.DTOs;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class AttendanceExportBuilderTests
{
    [Fact]
    public void Mobile_exports_are_utf8_valid_files_and_pdf_does_not_truncate_large_results()
    {
        var rows = Enumerable.Range(1, 46).Select(i => new AttendanceReportRowDto(Guid.NewGuid(), $"PER-{i:0000}", $"Personel {i}",
            "Bölüm", new DateOnly(2026, 7, 1).AddDays((i - 1) % 20), "Vardiya", "Complete", null, null, 480, 480, 0, 0)).ToArray();
        var report = new AttendanceReportDto(new(2026, 7, 1), new(2026, 7, 20), rows);

        var csv = AttendanceExportBuilder.Csv(report);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, csv[..3]);
        Assert.Contains("Sicil No;Personel;Bölüm", Encoding.UTF8.GetString(csv));

        var pdf = AttendanceExportBuilder.Pdf(report);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf[..8]));
        var pdfText = Encoding.Latin1.GetString(pdf);
        Assert.Equal(3, Regex.Matches(pdfText, @"/Type/Page(?!s)").Count);

        var xlsx = AttendanceExportBuilder.Xlsx(report);
        Assert.Equal((byte)'P', xlsx[0]);
        Assert.Equal((byte)'K', xlsx[1]);
        using var workbook = new ZipArchive(new MemoryStream(xlsx), ZipArchiveMode.Read);
        var sheet = Read(workbook, "xl/worksheets/sheet1.xml");
        Assert.Contains("state=\"frozen\"", sheet);
        Assert.Contains("ySplit=\"4\"", sheet);
        Assert.Contains("topLeftCell=\"A5\"", sheet);
        Assert.Contains("autoFilter ref=\"A4:N50\"", sheet);
        Assert.Contains("mergeCell ref=\"A1:N1\"", sheet);
        Assert.Contains("PUANTAJ RAPORU", sheet);
        Assert.Contains("Dönem:", sheet);
        Assert.Contains("PER-0046", sheet);
        Assert.Contains("Bölüm", sheet);
        Assert.NotNull(workbook.GetEntry("xl/styles.xml"));

        var englishCsv = Encoding.UTF8.GetString(AttendanceExportBuilder.Csv(report, true));
        Assert.Contains("Employee No;Employee;Department", englishCsv);
        var englishPdf = AttendanceExportBuilder.Pdf(report, true);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(englishPdf[..8]));
    }

    private static string Read(ZipArchive archive, string name)
    {
        using var reader = new StreamReader(archive.GetEntry(name)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

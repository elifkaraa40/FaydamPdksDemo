using System.Text;
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
        var pdf = Encoding.ASCII.GetString(AttendanceExportBuilder.Pdf(report));
        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.Contains("/Count 2", pdf);
        Assert.Contains("PER-0046", pdf);
        var xlsx = AttendanceExportBuilder.Xlsx(report);
        Assert.Equal((byte)'P', xlsx[0]);
        Assert.Equal((byte)'K', xlsx[1]);
    }
}

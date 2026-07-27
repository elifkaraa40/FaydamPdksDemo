using FaydamPDKS.Core.Enums;
using FaydamPDKS.Web;
using FaydamPDKS.Web.Models;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace FaydamPDKS.Tests;

public sealed class ReportTableFileBuilderTests
{
    [Fact]
    public void Transition_exports_contain_report_table_and_valid_file_headers()
    {
        var employeeId = Guid.NewGuid();
        var report = new TransitionReportViewModel(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 20),
            null, null,
            [new TransitionReportRow(1, employeeId, "PER-1", "Elif", "Operasyon", new DateOnly(2026, 7, 20),
                new TimeOnly(8, 30), "Giris", "Ana giriş", "İstanbul", "MobileQr")], []);

        var excel = ReportTableFileBuilder.TransitionsExcel(report);
        var pdf = ReportTableFileBuilder.TransitionsPdf(report);

        using var archive = new ZipArchive(new MemoryStream(excel));
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheet = reader.ReadToEnd();
        Assert.Contains("state=\"frozen\"", sheet);
        Assert.Contains("autoFilter", sheet);
        Assert.Contains("<t>08:30</t>", sheet);
        Assert.Contains("<t>MobileQr</t>", sheet);
        Assert.StartsWith("%PDF-1.4", Encoding.ASCII.GetString(pdf, 0, 8));
        SaveQaFile("TRANSITION_EXCEL_QA_PATH", excel);
        SaveQaFile("TRANSITION_PDF_QA_PATH", pdf);
    }

    [Fact]
    public void Leave_exports_contain_leave_columns_and_valid_file_headers()
    {
        var employeeId = Guid.NewGuid();
        var report = new LeaveReportViewModel(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 21),
            null, null, null,
            [new LeaveReportRow(Guid.NewGuid(), employeeId, "PER-1", "Elif", "Operasyon", LeaveType.Annual,
                new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 21), LeaveDayPortion.FullDay, 2,
                LeaveRequestStatus.Approved, "Yıllık izin")], []);

        var excel = ReportTableFileBuilder.LeavesExcel(report);
        var pdf = ReportTableFileBuilder.LeavesPdf(report);

        using var archive = new ZipArchive(new MemoryStream(excel));
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheet = reader.ReadToEnd();
        Assert.Contains("<t>İzin Türü</t>", sheet);
        Assert.Contains("<t>İş Günü</t>", sheet);
        Assert.Contains("<t>Onaylandı</t>", sheet);
        Assert.StartsWith("%PDF-1.4", Encoding.ASCII.GetString(pdf, 0, 8));
        SaveQaFile("LEAVE_EXCEL_QA_PATH", excel);
        SaveQaFile("LEAVE_PDF_QA_PATH", pdf);
    }

    private static void SaveQaFile(string environmentVariable, byte[] content)
    {
        var path = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(path)) File.WriteAllBytes(path, content);
    }
}

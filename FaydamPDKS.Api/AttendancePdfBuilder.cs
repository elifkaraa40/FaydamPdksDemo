using System.Globalization;
using FaydamPDKS.Core.DTOs;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace FaydamPDKS.Api;

internal static class AttendancePdfBuilder
{
    static AttendancePdfBuilder()
    {
        if (OperatingSystem.IsWindows())
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
    }

    public static byte[] Build(AttendanceReportDto report, bool english)
    {
        var pages = report.Rows.Chunk(20).ToArray();
        if (pages.Length == 0) pages = [[]];

        using var document = new PdfDocument();
        document.Info.Title = english ? "Attendance Report" : "Puantaj Raporu";
        document.Info.Author = "Faydam PDKS";
        for (var index = 0; index < pages.Length; index++)
            AddPage(document, report, pages[index], index + 1, pages.Length, english);

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static void AddPage(
        PdfDocument document,
        AttendanceReportDto report,
        AttendanceReportRowDto[] rows,
        int pageNumber,
        int pageCount,
        bool english)
    {
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        page.Orientation = PdfSharp.PageOrientation.Landscape;
        using var graphics = XGraphics.FromPdfPage(page);

        var unicode = new XPdfFontOptions(PdfFontEncoding.Unicode);
        var titleFont = new XFont("Arial", 17, XFontStyleEx.Bold, unicode);
        var subtitleFont = new XFont("Arial", 9, XFontStyleEx.Regular, unicode);
        var headerFont = new XFont("Arial", 7.5, XFontStyleEx.Bold, unicode);
        var bodyFont = new XFont("Arial", 7, XFontStyleEx.Regular, unicode);
        var footerFont = new XFont("Arial", 7, XFontStyleEx.Regular, unicode);

        var navy = new XSolidBrush(XColor.FromArgb(31, 47, 77));
        var headerBackground = new XSolidBrush(XColor.FromArgb(31, 71, 136));
        var headerText = XBrushes.White;
        var bodyText = new XSolidBrush(XColor.FromArgb(31, 41, 55));
        var mutedText = new XSolidBrush(XColor.FromArgb(95, 105, 120));
        var alternate = new XSolidBrush(XColor.FromArgb(243, 246, 250));
        var white = XBrushes.White;
        var grid = new XPen(XColor.FromArgb(205, 213, 224), 0.55);

        graphics.DrawString(
            english ? "ATTENDANCE REPORT" : "PUANTAJ RAPORU",
            titleFont,
            navy,
            new XRect(24, 22, 500, 24),
            XStringFormats.CenterLeft);
        graphics.DrawString(
            $"{report.From:dd.MM.yyyy} – {report.To:dd.MM.yyyy}",
            subtitleFont,
            mutedText,
            new XRect(24, 47, 300, 16),
            XStringFormats.CenterLeft);
        graphics.DrawString(
            english ? $"Page {pageNumber}/{pageCount}" : $"Sayfa {pageNumber}/{pageCount}",
            subtitleFont,
            mutedText,
            new XRect(page.Width.Point - 170, 25, 145, 18),
            XStringFormats.CenterRight);

        var headers = english
            ? new[] { "Employee No", "Employee", "Department", "Date", "Shift", "Work Location", "Entry", "Exit", "Worked", "Late/Overtime", "Status" }
            : new[] { "Sicil No", "Personel", "Bölüm", "Tarih", "Vardiya", "Çalışma Yeri", "Giriş", "Çıkış", "Çalışma", "Geç/Fazla", "Durum" };
        double[] widths = [58, 105, 60, 60, 76, 68, 46, 46, 52, 58, 105];
        const double left = 24;
        const double headerY = 78;
        const double rowHeight = 22;
        var tableWidth = widths.Sum();

        graphics.DrawRectangle(headerBackground, left, headerY, tableWidth, rowHeight);
        var x = left;
        for (var column = 0; column < headers.Length; column++)
        {
            graphics.DrawString(
                Fit(graphics, headers[column], headerFont, widths[column] - 8),
                headerFont,
                headerText,
                new XRect(x + 4, headerY, widths[column] - 8, rowHeight),
                XStringFormats.CenterLeft);
            x += widths[column];
        }

        if (rows.Length == 0)
        {
            graphics.DrawRectangle(alternate, left, headerY + rowHeight, tableWidth, rowHeight);
            graphics.DrawString(
                english ? "No attendance record was found in the selected range." : "Seçilen aralıkta puantaj kaydı bulunamadı.",
                subtitleFont,
                bodyText,
                new XRect(left + 6, headerY + rowHeight, tableWidth - 12, rowHeight),
                XStringFormats.CenterLeft);
        }

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = rows[rowIndex];
            var y = headerY + rowHeight * (rowIndex + 1);
            graphics.DrawRectangle(rowIndex % 2 == 0 ? alternate : white, left, y, tableWidth, rowHeight);
            string[] values =
            [
                row.EmployeeNumber,
                row.EmployeeName,
                row.Department ?? "—",
                row.WorkDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                row.ShiftName,
                WorkLocation(row, english),
                ShortTime(row.FirstEntry),
                ShortTime(row.LastExit),
                $"{row.WorkedMinutes} {(english ? "min" : "dk")}",
                $"{row.LateMinutes}/{row.OvertimeMinutes} {(english ? "min" : "dk")}",
                Status(row.Status, english)
            ];
            x = left;
            for (var column = 0; column < values.Length; column++)
            {
                graphics.DrawString(
                    Fit(graphics, values[column], bodyFont, widths[column] - 8),
                    bodyFont,
                    bodyText,
                    new XRect(x + 4, y, widths[column] - 8, rowHeight),
                    XStringFormats.CenterLeft);
                x += widths[column];
            }
        }

        var bottom = headerY + rowHeight * (Math.Max(1, rows.Length) + 1);
        x = left;
        for (var column = 0; column <= widths.Length; column++)
        {
            graphics.DrawLine(grid, x, headerY, x, bottom);
            if (column < widths.Length) x += widths[column];
        }
        for (var y = headerY; y <= bottom + 0.1; y += rowHeight)
            graphics.DrawLine(grid, left, y, left + tableWidth, y);

        graphics.DrawString(
            english ? $"Total {report.Rows.Count} records" : $"Toplam {report.Rows.Count} kayıt",
            footerFont,
            mutedText,
            new XRect(24, page.Height.Point - 28, 300, 14),
            XStringFormats.CenterLeft);
    }

    private static string Fit(XGraphics graphics, string value, XFont font, double width)
    {
        if (graphics.MeasureString(value, font).Width <= width) return value;
        const string suffix = "…";
        var length = value.Length;
        while (length > 1)
        {
            length--;
            var candidate = value[..length].TrimEnd() + suffix;
            if (graphics.MeasureString(candidate, font).Width <= width) return candidate;
        }
        return suffix;
    }

    private static string WorkLocation(AttendanceReportRowDto row, bool english)
    {
        var label = english
            ? row.WorkLocation switch { "Remote" => "Remote", "Field" => "Field", _ => "Office" }
            : row.WorkLocation switch { "Remote" => "Uzaktan", "Field" => "Saha", _ => "Ofis" };
        return string.IsNullOrWhiteSpace(row.WorkLocationDetail)
            ? label
            : $"{label} - {row.WorkLocationDetail}";
    }

    private static string Status(string status, bool english) => english
        ? status switch
        {
            "Complete" => "Completed",
            "NoRecord" => "No record",
            "NonWorkingDay" => "Non-working day",
            "MissingEntry" => "Missing entry",
            "MissingExit" => "Missing exit",
            "FieldWork" => "Field work",
            "RemoteWork" => "Remote work",
            _ => status
        }
        : status switch
        {
            "Complete" => "Tamamlandı",
            "NoRecord" => "Kayıt yok",
            "NonWorkingDay" => "Çalışma dışı gün",
            "MissingEntry" => "Giriş eksik",
            "MissingExit" => "Çıkış eksik",
            "FieldWork" => "Saha çalışması",
            "RemoteWork" => "Uzaktan çalışma",
            _ => status
        };

    private static string ShortTime(DateTimeOffset? value) =>
        value?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "—";
}

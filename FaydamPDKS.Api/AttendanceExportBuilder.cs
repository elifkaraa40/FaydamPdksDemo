using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Api;

public static class AttendanceExportBuilder
{
    public static byte[] Csv(AttendanceReportDto report, bool english = false)
    {
        var output = new StringBuilder();
        output.AppendLine(english
            ? "Employee No;Employee;Department;Date;Shift;Status;Work Location;Work Detail;First Entry;Last Exit;Worked Minutes;Expected Minutes;Late Minutes;Overtime Minutes"
            : "Sicil No;Personel;Bölüm;Tarih;Vardiya;Durum;Çalışma Şekli;Çalışma Detayı;İlk Giriş;Son Çıkış;Çalışılan Dakika;Beklenen Dakika;Geç Dakika;Fazla Mesai Dakika");
        foreach (var row in report.Rows)
            output.AppendJoin(';', Cell(row.EmployeeNumber), Cell(row.EmployeeName), Cell(row.Department),
                row.WorkDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), Cell(row.ShiftName), Cell(Status(row.Status, english)), Cell(WorkLocationLabel(row.WorkLocation, english)), Cell(row.WorkLocationDetail),
                Cell(row.FirstEntry?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)), Cell(row.LastExit?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)),
                row.WorkedMinutes, row.ExpectedMinutes, row.LateMinutes, row.OvertimeMinutes).AppendLine();
        var encoding = new UTF8Encoding(true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(output.ToString())).ToArray();
    }

    public static byte[] Pdf(AttendanceReportDto report, bool english = false) =>
        AttendancePdfBuilder.Build(report, english);

    private static StringBuilder BuildPdfPage(AttendanceReportDto report, AttendanceReportRowDto[] rows, int page, int pageCount)
    {
        string[] headers = ["Sicil No", "Personel", "Bolum", "Tarih", "Vardiya", "Calisma Yeri", "Giris", "Cikis", "Calisma", "Gec/Fazla", "Durum"];
        // Landscape A4: keep the complete table inside the printable area.
        double[] widths = [58, 105, 60, 60, 76, 68, 46, 46, 52, 58, 105];
        const double left = 24, rowHeight = 22;
        var content = new StringBuilder();
        content.Append("0.12 0.18 0.30 rg BT /F1 17 Tf 24 555 Td (PUANTAJ RAPORU) Tj ET\n")
            .Append($"0.35 0.40 0.48 rg BT /F1 9 Tf 24 537 Td ({report.From:dd.MM.yyyy} - {report.To:dd.MM.yyyy}) Tj ET\n")
            .Append($"BT /F1 8 Tf 760 555 Td (Sayfa {page}/{pageCount}) Tj ET\n");
        var y = 505d;
        var x = left;
        content.Append($"0.12 0.25 0.46 rg {left} {y} {widths.Sum()} {rowHeight} re f\n");
        for (var i = 0; i < headers.Length; i++)
        {
            content.Append($"1 1 1 rg BT /F1 8 Tf {x + 4:0.##} {y + 8:0.##} Td ({headers[i]}) Tj ET\n");
            x += widths[i];
        }
        if (rows.Length == 0)
        {
            y -= rowHeight;
            content.Append($"0.96 0.97 0.98 rg {left} {y} {widths.Sum()} {rowHeight} re f\n")
                .Append($"0.25 0.30 0.38 rg BT /F1 9 Tf {left + 6} {y + 8} Td (Secilen aralikta kayit bulunamadi.) Tj ET\n");
        }
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = rows[rowIndex]; y -= rowHeight;
            if (rowIndex % 2 == 0) content.Append($"0.96 0.97 0.99 rg {left} {y} {widths.Sum()} {rowHeight} re f\n");
            string[] values = [row.EmployeeNumber, row.EmployeeName, row.Department ?? "-", row.WorkDate.ToString("dd.MM.yyyy"), row.ShiftName,
                WorkLocation(row), ShortTime(row.FirstEntry), ShortTime(row.LastExit), $"{row.WorkedMinutes} dk", $"{row.LateMinutes}/{row.OvertimeMinutes} dk", Status(row.Status)];
            x = left;
            for (var i = 0; i < values.Length; i++)
            {
                var value = Fit(AsciiText(values[i]), widths[i]);
                content.Append($"0.12 0.16 0.23 rg BT /F1 7 Tf {x + 4:0.##} {y + 8:0.##} Td ({EscapePdf(value)}) Tj ET\n");
                x += widths[i];
            }
        }
        var tableBottom = y; x = left;
        content.Append("0.78 0.81 0.86 RG 0.5 w\n");
        for (var i = 0; i <= widths.Length; i++)
        {
            content.Append($"{x:0.##} {tableBottom:0.##} m {x:0.##} {505 + rowHeight:0.##} l S\n");
            if (i < widths.Length) x += widths[i];
        }
        for (var lineY = tableBottom; lineY <= 505 + rowHeight; lineY += rowHeight)
            content.Append($"{left} {lineY:0.##} m {left + widths.Sum():0.##} {lineY:0.##} l S\n");
        content.Append($"0.40 0.44 0.50 rg BT /F1 7 Tf 24 20 Td (Toplam {report.Rows.Count} kayit) Tj ET\n");
        return content;
    }

    public static byte[] Xlsx(AttendanceReportDto report, bool english = false) =>
        AttendanceExcelBuilder.Build(report, english);

    private static string Cell(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static void Entry(ZipArchive archive, string name, string content) { using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false)); writer.Write(content); }
    private static void Row(StringBuilder sheet, IEnumerable<string?> values) { sheet.Append("<row>"); foreach (var value in values) sheet.Append("<c t=\"inlineStr\"><is><t>").Append(SecurityElement.Escape(value ?? string.Empty)).Append("</t></is></c>"); sheet.Append("</row>"); }
    private static void WriteAscii(Stream stream, string value) => stream.Write(Encoding.ASCII.GetBytes(value));
    private static string EscapePdf(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Ascii(string value) => value.Replace('ç', 'c').Replace('Ç', 'C').Replace('ğ', 'g').Replace('Ğ', 'G').Replace('ı', 'i').Replace('İ', 'I').Replace('ö', 'o').Replace('Ö', 'O').Replace('ş', 's').Replace('Ş', 'S').Replace('ü', 'u').Replace('Ü', 'U');
    private static string Fit(string value, double width) { var max = Math.Max(4, (int)(width / 4.2)); return value.Length <= max ? value : value[..(max - 3)] + "..."; }
    private static string AsciiText(string value) => Ascii(value);
    private static string ShortTime(DateTimeOffset? value) => value?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "--";
    private static string WorkLocation(AttendanceReportRowDto row) => string.IsNullOrWhiteSpace(row.WorkLocationDetail) ? WorkLocationLabel(row.WorkLocation) : $"{WorkLocationLabel(row.WorkLocation)} - {row.WorkLocationDetail}";
    private static string WorkLocationLabel(string location, bool english = false) => english
        ? location switch { "Remote" => "Remote", "Field" => "Field", _ => "Office" }
        : location switch { "Remote" => "Uzaktan", "Field" => "Saha", _ => "Ofis" };
    private static string Status(string status, bool english = false) => english
        ? status switch { "Complete" => "Completed", "NoRecord" => "No record", "NonWorkingDay" => "Non-working day", "MissingEntry" => "Missing entry", "MissingExit" => "Missing exit", "FieldWork" => "Field work", "RemoteWork" => "Remote work", _ => status }
        : status switch { "Complete" => "Tamamlandı", "NoRecord" => "Kayıt yok", "NonWorkingDay" => "Çalışma dışı gün", "MissingEntry" => "Giriş eksik", "MissingExit" => "Çıkış eksik", "FieldWork" => "Saha çalışması", "RemoteWork" => "Uzaktan çalışma", _ => status };
}

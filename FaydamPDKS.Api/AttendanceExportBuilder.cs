using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Api;

public static class AttendanceExportBuilder
{
    public static byte[] Csv(AttendanceReportDto report)
    {
        var output = new StringBuilder();
        output.AppendLine("Sicil No;Personel;Bölüm;Tarih;Vardiya;Durum;Çalışma Şekli;Çalışma Detayı;İlk Giriş;Son Çıkış;Çalışılan Dakika;Beklenen Dakika;Geç Dakika;Fazla Mesai Dakika");
        foreach (var row in report.Rows)
            output.AppendJoin(';', Cell(row.EmployeeNumber), Cell(row.EmployeeName), Cell(row.Department),
                row.WorkDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), Cell(row.ShiftName), Cell(Status(row.Status)), Cell(WorkLocationLabel(row.WorkLocation)), Cell(row.WorkLocationDetail),
                Cell(row.FirstEntry?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)), Cell(row.LastExit?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)),
                row.WorkedMinutes, row.ExpectedMinutes, row.LateMinutes, row.OvertimeMinutes).AppendLine();
        var encoding = new UTF8Encoding(true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(output.ToString())).ToArray();
    }

    public static byte[] Pdf(AttendanceReportDto report)
    {
        var pages = report.Rows.Chunk(20).ToArray();
        if (pages.Length == 0) pages = [[]];
        var objects = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", string.Empty, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        var pageIds = new List<int>();
        for (var pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            var pageId = objects.Count + 1; pageIds.Add(pageId); objects.Add(string.Empty);
            var contentId = objects.Count + 1;
            var content = BuildPdfPage(report, pages[pageIndex], pageIndex + 1, pages.Length);
            var stream = Encoding.ASCII.GetBytes(content.ToString());
            objects.Add($"<< /Length {stream.Length} >>\nstream\n{Encoding.ASCII.GetString(stream)}\nendstream");
            // Web report uses a landscape A4 page; keep the mobile export identical.
            objects[pageId - 1] = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>";
        }
        objects[1] = $"<< /Type /Pages /Count {pageIds.Count} /Kids [{string.Join(' ', pageIds.Select(x => $"{x} 0 R"))}] >>";
        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++) { offsets.Add(output.Position); WriteAscii(output, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = output.Position;
        WriteAscii(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) WriteAscii(output, $"{offset:0000000000} 00000 n \n");
        WriteAscii(output, $"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

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

    public static byte[] Xlsx(AttendanceReportDto report)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Entry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Entry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Entry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Puantaj\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Entry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView><pane ySplit=\"1\" topLeftCell=\"A2\" state=\"frozen\"/></sheetView></sheetViews><sheetData>");
            Row(sheet, ["Sicil No", "Personel", "Bölüm", "Tarih", "Vardiya", "Durum", "Çalışma Şekli", "Çalışma Detayı", "İlk Giriş", "Son Çıkış", "Çalışılan Dakika", "Beklenen Dakika", "Geç Dakika", "Fazla Mesai Dakika"]);
            foreach (var x in report.Rows) Row(sheet, [x.EmployeeNumber, x.EmployeeName, x.Department, x.WorkDate.ToString("yyyy-MM-dd"), x.ShiftName,
                Status(x.Status), WorkLocationLabel(x.WorkLocation), x.WorkLocationDetail, x.FirstEntry?.ToString("dd.MM.yyyy HH:mm"), x.LastExit?.ToString("dd.MM.yyyy HH:mm"), x.WorkedMinutes.ToString(), x.ExpectedMinutes.ToString(), x.LateMinutes.ToString(), x.OvertimeMinutes.ToString()]);
            sheet.Append("</sheetData><autoFilter ref=\"A1:N").Append(report.Rows.Count + 1).Append("\"/></worksheet>");
            Entry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return output.ToArray();
    }

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
    private static string WorkLocationLabel(string location) => location switch { "Remote" => "Uzaktan", "Field" => "Saha", _ => "Ofis" };
    private static string Status(string status) => status switch { "Complete" => "Tamamlandı", "NoRecord" => "Kayıt yok", "NonWorkingDay" => "Çalışma dışı gün", "MissingEntry" => "Giriş eksik", "MissingExit" => "Çıkış eksik", "FieldWork" => "Saha çalışması", "RemoteWork" => "Uzaktan çalışma", _ => status };
}

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
        output.AppendLine("Sicil No;Personel;Bölüm;Tarih;Vardiya;Durum;Çalışma Konumu;İlk Giriş;Son Çıkış;Çalışılan Dakika;Beklenen Dakika;Geç Dakika;Fazla Mesai Dakika");
        foreach (var row in report.Rows)
            output.AppendJoin(';', Cell(row.EmployeeNumber), Cell(row.EmployeeName), Cell(row.Department),
                row.WorkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Cell(row.ShiftName), Cell(row.Status), Cell(row.WorkLocation),
                Cell(row.FirstEntry?.ToString("O", CultureInfo.InvariantCulture)), Cell(row.LastExit?.ToString("O", CultureInfo.InvariantCulture)),
                row.WorkedMinutes, row.ExpectedMinutes, row.LateMinutes, row.OvertimeMinutes).AppendLine();
        var encoding = new UTF8Encoding(true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(output.ToString())).ToArray();
    }

    public static byte[] Pdf(AttendanceReportDto report)
    {
        var pages = report.Rows.Chunk(45).ToArray();
        if (pages.Length == 0) pages = [[]];
        var objects = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", string.Empty, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        var pageIds = new List<int>();
        for (var pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            var pageId = objects.Count + 1; pageIds.Add(pageId); objects.Add(string.Empty);
            var contentId = objects.Count + 1;
            var content = new StringBuilder("BT /F1 12 Tf 40 800 Td (PUANTAJ RAPORU) Tj 0 -20 Td ")
                .Append($"({report.From:yyyy-MM-dd} - {report.To:yyyy-MM-dd}  Sayfa {pageIndex + 1}/{pages.Length}) Tj 0 -20 Td ");
            foreach (var row in pages[pageIndex])
                content.Append($"({EscapePdf($"{row.WorkDate:yyyy-MM-dd} {Ascii(row.EmployeeNumber)} {Ascii(row.EmployeeName)} {Ascii(row.Status)}")}) Tj 0 -15 Td ");
            content.Append("ET");
            var stream = Encoding.ASCII.GetBytes(content.ToString());
            objects.Add($"<< /Length {stream.Length} >>\nstream\n{Encoding.ASCII.GetString(stream)}\nendstream");
            objects[pageId - 1] = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>";
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

    public static byte[] Xlsx(AttendanceReportDto report)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Entry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Entry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Entry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Puantaj\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Entry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            Row(sheet, ["Sicil No", "Personel", "Bölüm", "Tarih", "Vardiya", "Durum", "Çalışma Konumu", "İlk Giriş", "Son Çıkış", "Çalışılan Dakika", "Beklenen Dakika", "Geç Dakika", "Fazla Mesai Dakika"]);
            foreach (var x in report.Rows) Row(sheet, [x.EmployeeNumber, x.EmployeeName, x.Department, x.WorkDate.ToString("yyyy-MM-dd"), x.ShiftName,
                x.Status, x.WorkLocation, x.FirstEntry?.ToString("O"), x.LastExit?.ToString("O"), x.WorkedMinutes.ToString(), x.ExpectedMinutes.ToString(), x.LateMinutes.ToString(), x.OvertimeMinutes.ToString()]);
            sheet.Append("</sheetData></worksheet>");
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
}

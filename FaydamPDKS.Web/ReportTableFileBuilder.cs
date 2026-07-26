using FaydamPDKS.Core;
using FaydamPDKS.Web.Models;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace FaydamPDKS.Web;

public static class ReportTableFileBuilder
{
    public static byte[] TransitionsExcel(TransitionReportViewModel report) => Excel(
        "QR Hareketleri", "QR Hareketleri",
        ["Tarih", "Saat", "Sicil No", "Personel", "Bölüm", "İşlem", "İşyeri", "Bölge", "Kaynak"],
        report.Rows.Select(x => new[]
        {
            x.WorkDate.ToString("dd.MM.yyyy"), x.EventTime.ToString("HH:mm"), x.EmployeeNumber,
            x.EmployeeName, x.Department ?? "", EventType(x.EventType), x.WorkplaceName ?? "", x.ZoneName, x.Source
        }).ToArray(),
        [13, 10, 15, 24, 20, 12, 20, 22, 16]);

    public static byte[] TransitionsPdf(TransitionReportViewModel report) => Pdf(
        "QR HAREKETLERI", $"{report.From:dd.MM.yyyy} - {report.To:dd.MM.yyyy}",
        ["Tarih", "Saat", "Sicil", "Personel", "Bolum", "Islem", "Isyeri", "Bolge", "Kaynak"],
        report.Rows.Select(x => new[]
        {
            x.WorkDate.ToString("dd.MM.yyyy"), x.EventTime.ToString("HH:mm"), x.EmployeeNumber,
            x.EmployeeName, x.Department ?? "-", EventType(x.EventType), x.WorkplaceName ?? "-", x.ZoneName, x.Source
        }).ToArray(),
        [60, 40, 65, 105, 85, 48, 85, 105, 70]);

    public static byte[] LeavesExcel(LeaveReportViewModel report) => Excel(
        "İzin Raporu", "İzin Raporu",
        ["Sicil No", "Personel", "Bölüm", "Başlangıç", "Bitiş", "İzin Türü", "Gün Türü", "İş Günü", "Durum", "Açıklama"],
        report.Rows.Select(x => new[]
        {
            x.EmployeeNumber, x.EmployeeName, x.Department ?? "", x.StartDate.ToString("dd.MM.yyyy"),
            x.EndDate.ToString("dd.MM.yyyy"), LeaveLabels.Type(x.LeaveType), LeaveLabels.Portion(x.DayPortion),
            x.WorkDayCount.ToString("0.#", CultureInfo.GetCultureInfo("tr-TR")), LeaveLabels.Status(x.Status), x.Reason ?? ""
        }).ToArray(),
        [15, 24, 20, 13, 13, 18, 18, 12, 15, 32]);

    public static byte[] LeavesPdf(LeaveReportViewModel report) => Pdf(
        "IZIN RAPORU", $"{report.From:dd.MM.yyyy} - {report.To:dd.MM.yyyy}",
        ["Sicil", "Personel", "Bolum", "Baslangic", "Bitis", "Izin Turu", "Gun Turu", "Is Gunu", "Durum", "Aciklama"],
        report.Rows.Select(x => new[]
        {
            x.EmployeeNumber, x.EmployeeName, x.Department ?? "-", x.StartDate.ToString("dd.MM.yyyy"),
            x.EndDate.ToString("dd.MM.yyyy"), LeaveLabels.Type(x.LeaveType), LeaveLabels.Portion(x.DayPortion),
            x.WorkDayCount.ToString("0.#", CultureInfo.GetCultureInfo("tr-TR")), LeaveLabels.Status(x.Status), x.Reason ?? "-"
        }).ToArray(),
        [58, 90, 75, 62, 62, 75, 70, 50, 65, 110]);

    private static byte[] Excel(string title, string sheetName, string[] headers, string[][] rows, double[] widths)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/><Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/><Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/></Types>
                """);
            WriteEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/><Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/></Relationships>
                """);
            WriteEntry(archive, "docProps/core.xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:title>{title}</dc:title><dc:creator>Faydam PDKS</dc:creator><cp:lastModifiedBy>Faydam PDKS</cp:lastModifiedBy></cp:coreProperties>
                """);
            WriteEntry(archive, "docProps/app.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"><Application>Faydam PDKS</Application><DocSecurity>0</DocSecurity><ScaleCrop>false</ScaleCrop></Properties>
                """);
            WriteEntry(archive, "xl/workbook.xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><bookViews><workbookView/></bookViews><sheets><sheet name="{sheetName}" sheetId="1" r:id="rId1"/></sheets><calcPr calcId="191029"/></workbook>
                """);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
                """);
            WriteEntry(archive, "xl/styles.xml", ExcelStyles);

            var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
            using var stream = entry.Open();
            using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
            writer.WriteStartDocument(true);
            writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            var lastColumn = ColumnName(headers.Length - 1);
            writer.WriteStartElement("dimension"); writer.WriteAttributeString("ref", $"A1:{lastColumn}{rows.Length + 1}"); writer.WriteEndElement();
            writer.WriteStartElement("sheetViews"); writer.WriteStartElement("sheetView"); writer.WriteAttributeString("showGridLines", "0"); writer.WriteAttributeString("workbookViewId", "0");
            writer.WriteStartElement("pane"); writer.WriteAttributeString("ySplit", "1"); writer.WriteAttributeString("topLeftCell", "A2"); writer.WriteAttributeString("activePane", "bottomLeft"); writer.WriteAttributeString("state", "frozen"); writer.WriteEndElement();
            writer.WriteEndElement(); writer.WriteEndElement();
            writer.WriteStartElement("cols");
            for (var index = 0; index < widths.Length; index++) { writer.WriteStartElement("col"); writer.WriteAttributeString("min", (index + 1).ToString()); writer.WriteAttributeString("max", (index + 1).ToString()); writer.WriteAttributeString("width", widths[index].ToString(CultureInfo.InvariantCulture)); writer.WriteAttributeString("customWidth", "1"); writer.WriteEndElement(); }
            writer.WriteEndElement();
            writer.WriteStartElement("sheetData");
            WriteExcelRow(writer, headers, 1, 1);
            for (var index = 0; index < rows.Length; index++) WriteExcelRow(writer, rows[index], index % 2 == 0 ? 2 : 3, index + 2);
            writer.WriteEndElement();
            writer.WriteStartElement("autoFilter"); writer.WriteAttributeString("ref", $"A1:{lastColumn}{rows.Length + 1}"); writer.WriteEndElement();
            writer.WriteStartElement("pageMargins"); writer.WriteAttributeString("left", "0.25"); writer.WriteAttributeString("right", "0.25"); writer.WriteAttributeString("top", "0.5"); writer.WriteAttributeString("bottom", "0.5"); writer.WriteAttributeString("header", "0.2"); writer.WriteAttributeString("footer", "0.2"); writer.WriteEndElement();
            writer.WriteStartElement("pageSetup"); writer.WriteAttributeString("orientation", "landscape"); writer.WriteAttributeString("fitToWidth", "1"); writer.WriteAttributeString("fitToHeight", "0"); writer.WriteEndElement();
            writer.WriteEndElement(); writer.WriteEndDocument();
        }
        return output.ToArray();
    }

    private static byte[] Pdf(string title, string subtitle, string[] headers, string[][] rows, double[] widths)
    {
        var chunks = rows.Chunk(20).ToArray();
        if (chunks.Length == 0) chunks = [[]];
        var objects = new List<byte[]> { Ascii("<< /Type /Catalog /Pages 2 0 R >>"), Array.Empty<byte>(), Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>") };
        var pageIds = new List<int>(); var contentIds = new List<int>();
        for (var pageIndex = 0; pageIndex < chunks.Length; pageIndex++)
        {
            pageIds.Add(objects.Count + 1); objects.Add(Array.Empty<byte>()); contentIds.Add(objects.Count + 1);
            var content = BuildPdfPage(title, subtitle, headers, chunks[pageIndex], widths, pageIndex + 1, chunks.Length, rows.Length);
            var bytes = Ascii(content);
            objects.Add(Ascii($"<< /Length {bytes.Length} >>\nstream\n{Encoding.ASCII.GetString(bytes)}\nendstream"));
        }
        objects[1] = Ascii($"<< /Type /Pages /Count {pageIds.Count} /Kids [{string.Join(' ', pageIds.Select(x => $"{x} 0 R"))}] >>");
        for (var i = 0; i < pageIds.Count; i++) objects[pageIds[i] - 1] = Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentIds[i]} 0 R >>");
        using var output = new MemoryStream(); Write(output, "%PDF-1.4\n"); var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++) { offsets.Add(output.Position); Write(output, $"{i + 1} 0 obj\n"); output.Write(objects[i]); Write(output, "\nendobj\n"); }
        var xref = output.Position; Write(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); return output.ToArray();
    }

    private static string BuildPdfPage(string title, string subtitle, string[] headers, string[][] rows, double[] widths, int page, int pageCount, int total)
    {
        const double left = 24, rowHeight = 22; var content = new StringBuilder();
        content.Append($"0.12 0.18 0.30 rg BT /F1 17 Tf 24 555 Td ({PdfEscape(ToAscii(title))}) Tj ET\n")
            .Append($"0.35 0.40 0.48 rg BT /F1 9 Tf 24 537 Td ({PdfEscape(ToAscii(subtitle))}) Tj ET\n")
            .Append($"BT /F1 8 Tf 760 555 Td (Sayfa {page}/{pageCount}) Tj ET\n");
        var y = 505d; var x = left; content.Append($"0.12 0.25 0.46 rg {left} {y} {widths.Sum()} {rowHeight} re f\n");
        for (var i = 0; i < headers.Length; i++) { content.Append($"1 1 1 rg BT /F1 8 Tf {x + 4:0.##} {y + 8:0.##} Td ({PdfEscape(Fit(ToAscii(headers[i]), widths[i]))}) Tj ET\n"); x += widths[i]; }
        if (rows.Length == 0) { y -= rowHeight; content.Append($"0.96 0.97 0.98 rg {left} {y} {widths.Sum()} {rowHeight} re f\n0.25 0.30 0.38 rg BT /F1 9 Tf {left + 6} {y + 8} Td (Secilen aralikta kayit bulunamadi.) Tj ET\n"); }
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            y -= rowHeight; if (rowIndex % 2 == 0) content.Append($"0.96 0.97 0.99 rg {left} {y} {widths.Sum()} {rowHeight} re f\n"); x = left;
            for (var i = 0; i < rows[rowIndex].Length; i++) { content.Append($"0.12 0.16 0.23 rg BT /F1 7 Tf {x + 4:0.##} {y + 8:0.##} Td ({PdfEscape(Fit(ToAscii(rows[rowIndex][i]), widths[i]))}) Tj ET\n"); x += widths[i]; }
        }
        var bottom = y; x = left; content.Append("0.78 0.81 0.86 RG 0.5 w\n");
        for (var i = 0; i <= widths.Length; i++) { content.Append($"{x:0.##} {bottom:0.##} m {x:0.##} {505 + rowHeight:0.##} l S\n"); if (i < widths.Length) x += widths[i]; }
        for (var lineY = bottom; lineY <= 505 + rowHeight; lineY += rowHeight) content.Append($"{left} {lineY:0.##} m {left + widths.Sum():0.##} {lineY:0.##} l S\n");
        content.Append($"0.40 0.44 0.50 rg BT /F1 7 Tf 24 20 Td (Toplam {total} kayit) Tj ET\n"); return content.ToString();
    }

    private static void WriteExcelRow(XmlWriter writer, string[] values, int style, int rowNumber)
    {
        writer.WriteStartElement("row"); writer.WriteAttributeString("r", rowNumber.ToString()); writer.WriteAttributeString("ht", rowNumber == 1 ? "28" : "22"); writer.WriteAttributeString("customHeight", "1");
        for (var index = 0; index < values.Length; index++) { writer.WriteStartElement("c"); writer.WriteAttributeString("r", $"{ColumnName(index)}{rowNumber}"); writer.WriteAttributeString("t", "inlineStr"); writer.WriteAttributeString("s", style.ToString()); writer.WriteStartElement("is"); writer.WriteElementString("t", values[index]); writer.WriteEndElement(); writer.WriteEndElement(); }
        writer.WriteEndElement();
    }

    private static string ColumnName(int index) { var result = ""; for (var value = index + 1; value > 0; value = (value - 1) / 26) result = (char)('A' + (value - 1) % 26) + result; return result; }
    private static void WriteEntry(ZipArchive archive, string name, string content) { var entry = archive.CreateEntry(name, CompressionLevel.Fastest); using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)); writer.Write(content); }
    private static string Fit(string value, double width) { var max = Math.Max(4, (int)(width / 4.2)); return value.Length <= max ? value : value[..(max - 3)] + "..."; }
    private static string EventType(string value) => value switch { "Giris" => "Giriş", "Cikis" => "Çıkış", _ => value };
    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
    private static void Write(Stream stream, string value) => stream.Write(Ascii(value));
    private static string PdfEscape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string ToAscii(string value) => value.Replace('ç', 'c').Replace('Ç', 'C').Replace('ğ', 'g').Replace('Ğ', 'G').Replace('ı', 'i').Replace('İ', 'I').Replace('ö', 'o').Replace('Ö', 'O').Replace('ş', 's').Replace('Ş', 'S').Replace('ü', 'u').Replace('Ü', 'U');

    private const string ExcelStyles = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="10"/><name val="Aptos"/><color rgb="FF1F2937"/></font><font><b/><sz val="10"/><name val="Aptos Display"/><color rgb="FFFFFFFF"/></font></fonts><fills count="5"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF1F4788"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFF3F6FA"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFFFFFFF"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="2"><border/><border><left/><right/><top/><bottom style="thin"><color rgb="FFD7DEE8"/></bottom><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="4"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf><xf numFmtId="0" fontId="0" fillId="3" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf><xf numFmtId="0" fontId="0" fillId="4" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>
        """;
}

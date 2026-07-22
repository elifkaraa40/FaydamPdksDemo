using FaydamPDKS.Core.DTOs;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace FaydamPDKS.Web;

public static class AttendanceReportFileBuilder
{
    private static readonly string[] Headers =
    [
        "Sicil No", "Personel", "Bölüm", "Tarih", "Vardiya", "Durum", "Çalışma Şekli", "Çalışma Detayı", "İlk Giriş", "Son Çıkış",
        "Çalışılan Dakika", "Beklenen Dakika", "Geç Dakika", "Fazla Mesai Dakika"
    ];

    public static byte[] Excel(AttendanceReportDto report)
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
            WriteEntry(archive, "docProps/core.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><dc:title>Puantaj Raporu</dc:title><dc:creator>Faydam PDKS</dc:creator><cp:lastModifiedBy>Faydam PDKS</cp:lastModifiedBy></cp:coreProperties>
                """);
            WriteEntry(archive, "docProps/app.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"><Application>Faydam PDKS</Application><DocSecurity>0</DocSecurity><ScaleCrop>false</ScaleCrop><HeadingPairs><vt:vector size="2" baseType="variant"><vt:variant><vt:lpstr>Çalışma Sayfaları</vt:lpstr></vt:variant><vt:variant><vt:i4>1</vt:i4></vt:variant></vt:vector></HeadingPairs><TitlesOfParts><vt:vector size="1" baseType="lpstr"><vt:lpstr>Puantaj</vt:lpstr></vt:vector></TitlesOfParts></Properties>
                """);
            WriteEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><bookViews><workbookView/></bookViews><sheets><sheet name="Puantaj" sheetId="1" r:id="rId1"/></sheets><calcPr calcId="191029"/></workbook>
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
            writer.WriteStartElement("dimension"); writer.WriteAttributeString("ref", $"A1:N{report.Rows.Count + 1}"); writer.WriteEndElement();
            writer.WriteStartElement("sheetViews"); writer.WriteStartElement("sheetView"); writer.WriteAttributeString("showGridLines", "0"); writer.WriteAttributeString("workbookViewId", "0");
            writer.WriteStartElement("pane"); writer.WriteAttributeString("ySplit", "1"); writer.WriteAttributeString("topLeftCell", "A2"); writer.WriteAttributeString("activePane", "bottomLeft"); writer.WriteAttributeString("state", "frozen"); writer.WriteEndElement();
            writer.WriteEndElement(); writer.WriteEndElement();
            writer.WriteStartElement("sheetFormatPr"); writer.WriteAttributeString("defaultRowHeight", "15"); writer.WriteEndElement();
            writer.WriteStartElement("cols");
            double[] widths = [14, 24, 20, 13, 23, 18, 18, 28, 18, 18, 19, 17, 14, 22];
            for (var index = 0; index < widths.Length; index++) { writer.WriteStartElement("col"); writer.WriteAttributeString("min", (index + 1).ToString()); writer.WriteAttributeString("max", (index + 1).ToString()); writer.WriteAttributeString("width", widths[index].ToString(CultureInfo.InvariantCulture)); writer.WriteAttributeString("customWidth", "1"); writer.WriteEndElement(); }
            writer.WriteEndElement();
            writer.WriteStartElement("sheetData");
            WriteExcelHeader(writer);
            for (var index = 0; index < report.Rows.Count; index++) WriteExcelDataRow(writer, report.Rows[index], index % 2 == 0, index + 2);
            writer.WriteEndElement();
            writer.WriteStartElement("autoFilter"); writer.WriteAttributeString("ref", $"A1:N{report.Rows.Count + 1}"); writer.WriteEndElement();
            writer.WriteStartElement("pageMargins"); writer.WriteAttributeString("left", "0.25"); writer.WriteAttributeString("right", "0.25"); writer.WriteAttributeString("top", "0.5"); writer.WriteAttributeString("bottom", "0.5"); writer.WriteAttributeString("header", "0.2"); writer.WriteAttributeString("footer", "0.2"); writer.WriteEndElement();
            writer.WriteStartElement("pageSetup"); writer.WriteAttributeString("orientation", "landscape"); writer.WriteAttributeString("fitToWidth", "1"); writer.WriteAttributeString("fitToHeight", "0"); writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        return output.ToArray();
    }

    public static byte[] Pdf(AttendanceReportDto report)
    {
        var pages = report.Rows.Chunk(20).ToArray();
        if (pages.Length == 0) pages = [[]];
        var objects = new List<byte[]>();
        var pageIds = new List<int>();
        var contentIds = new List<int>();
        const int catalogId = 1, pagesId = 2, fontId = 3;
        objects.Add(Ascii("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add([]); // Filled after page object ids are known.
        objects.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));
        for (var pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            pageIds.Add(objects.Count + 1);
            objects.Add([]);
            contentIds.Add(objects.Count + 1);
            var content = BuildPdfPage(report, pages[pageIndex], pageIndex + 1, pages.Length);
            var bytes = Ascii(content.ToString());
            objects.Add(Ascii($"<< /Length {bytes.Length} >>\nstream\n{Encoding.ASCII.GetString(bytes)}\nendstream"));
        }
        objects[pagesId - 1] = Ascii($"<< /Type /Pages /Count {pageIds.Count} /Kids [{string.Join(' ', pageIds.Select(x => $"{x} 0 R"))}] >>");
        for (var i = 0; i < pageIds.Count; i++)
            objects[pageIds[i] - 1] = Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentIds[i]} 0 R >>");

        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            Write(output, $"{i + 1} 0 obj\n"); output.Write(objects[i]); Write(output, "\nendobj\n");
        }
        var xref = output.Position;
        Write(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer << /Size {objects.Count + 1} /Root {catalogId} 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static string BuildPdfPage(AttendanceReportDto report, AttendanceReportRowDto[] rows, int page, int pageCount)
    {
        string[] headers = ["Sicil No", "Personel", "Bolum", "Tarih", "Vardiya", "Calisma Yeri", "Giris", "Cikis", "Calisma", "Gec/Fazla", "Durum"];
        double[] widths = [64, 115, 65, 62, 80, 70, 48, 48, 55, 55, 104];
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
            var row = rows[rowIndex];
            y -= rowHeight;
            if (rowIndex % 2 == 0) content.Append($"0.96 0.97 0.99 rg {left} {y} {widths.Sum()} {rowHeight} re f\n");
            string[] values =
            [
                row.EmployeeNumber, row.EmployeeName, row.Department ?? "-", row.WorkDate.ToString("dd.MM.yyyy"), row.ShiftName,
                WorkLocation(row), ShortTime(row.FirstEntry), ShortTime(row.LastExit), $"{row.WorkedMinutes} dk", $"{row.LateMinutes}/{row.OvertimeMinutes} dk", Status(row.Status)
            ];
            x = left;
            for (var i = 0; i < values.Length; i++)
            {
                var value = Fit(ToAscii(values[i]), widths[i]);
                content.Append($"0.12 0.16 0.23 rg BT /F1 7 Tf {x + 4:0.##} {y + 8:0.##} Td ({PdfEscape(value)}) Tj ET\n");
                x += widths[i];
            }
        }

        var tableBottom = y;
        x = left;
        content.Append("0.78 0.81 0.86 RG 0.5 w\n");
        for (var i = 0; i <= widths.Length; i++)
        {
            content.Append($"{x:0.##} {tableBottom:0.##} m {x:0.##} {505 + rowHeight:0.##} l S\n");
            if (i < widths.Length) x += widths[i];
        }
        for (var lineY = tableBottom; lineY <= 505 + rowHeight; lineY += rowHeight)
            content.Append($"{left} {lineY:0.##} m {left + widths.Sum():0.##} {lineY:0.##} l S\n");
        content.Append($"0.40 0.44 0.50 rg BT /F1 7 Tf 24 20 Td (Toplam {report.Rows.Count} kayit) Tj ET\n");
        return content.ToString();
    }

    private static string Fit(string value, double width)
    {
        var max = Math.Max(4, (int)(width / 4.2));
        return value.Length <= max ? value : value[..(max - 3)] + "...";
    }

    private static void WriteExcelHeader(XmlWriter writer)
    {
        writer.WriteStartElement("row"); writer.WriteAttributeString("r", "1"); writer.WriteAttributeString("ht", "28"); writer.WriteAttributeString("customHeight", "1");
        for (var index = 0; index < Headers.Length; index++) WriteTextCell(writer, Headers[index], 1, $"{ColumnName(index)}1");
        writer.WriteEndElement();
    }

    private static void WriteExcelDataRow(XmlWriter writer, AttendanceReportRowDto row, bool alternate, int rowNumber)
    {
        var textStyle = alternate ? 2 : 3;
        var dateStyle = alternate ? 4 : 5;
        var numberStyle = alternate ? 6 : 7;
        var statusStyle = row.Status == "Complete" ? 8 : row.Status is "NoRecord" or "NonWorkingDay" ? 9 : 10;
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", rowNumber.ToString()); writer.WriteAttributeString("ht", "22"); writer.WriteAttributeString("customHeight", "1");
        WriteTextCell(writer, row.EmployeeNumber, textStyle, $"A{rowNumber}");
        WriteTextCell(writer, row.EmployeeName, textStyle, $"B{rowNumber}");
        WriteTextCell(writer, row.Department, textStyle, $"C{rowNumber}");
        WriteNumberCell(writer, row.WorkDate.ToDateTime(TimeOnly.MinValue).ToOADate(), dateStyle, $"D{rowNumber}");
        WriteTextCell(writer, row.ShiftName, textStyle, $"E{rowNumber}");
        WriteTextCell(writer, Status(row.Status), statusStyle, $"F{rowNumber}");
        WriteTextCell(writer, WorkLocationLabel(row.WorkLocation), textStyle, $"G{rowNumber}");
        WriteTextCell(writer, row.WorkLocationDetail, textStyle, $"H{rowNumber}");
        WriteTextCell(writer, row.FirstEntry?.ToString("dd.MM.yyyy HH:mm"), textStyle, $"I{rowNumber}");
        WriteTextCell(writer, row.LastExit?.ToString("dd.MM.yyyy HH:mm"), textStyle, $"J{rowNumber}");
        WriteNumberCell(writer, row.WorkedMinutes, numberStyle, $"K{rowNumber}");
        WriteNumberCell(writer, row.ExpectedMinutes, numberStyle, $"L{rowNumber}");
        WriteNumberCell(writer, row.LateMinutes, numberStyle, $"M{rowNumber}");
        WriteNumberCell(writer, row.OvertimeMinutes, numberStyle, $"N{rowNumber}");
        writer.WriteEndElement();
    }

    private static void WriteTextCell(XmlWriter writer, string? value, int style, string reference)
    {
        writer.WriteStartElement("c"); writer.WriteAttributeString("r", reference); writer.WriteAttributeString("t", "inlineStr"); writer.WriteAttributeString("s", style.ToString());
        writer.WriteStartElement("is"); writer.WriteElementString("t", value ?? string.Empty); writer.WriteEndElement(); writer.WriteEndElement();
    }

    private static void WriteNumberCell(XmlWriter writer, double value, int style, string reference)
    {
        writer.WriteStartElement("c"); writer.WriteAttributeString("r", reference); writer.WriteAttributeString("s", style.ToString()); writer.WriteElementString("v", value.ToString(CultureInfo.InvariantCulture)); writer.WriteEndElement();
    }

    private static string ColumnName(int index) => ((char)('A' + index)).ToString();

    private const string ExcelStyles = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <numFmts count="1"><numFmt numFmtId="165" formatCode="#,##0 &quot;dk&quot;"/></numFmts>
          <fonts count="3"><font><sz val="10"/><name val="Aptos"/><color rgb="FF1F2937"/></font><font><b/><sz val="10"/><name val="Aptos Display"/><color rgb="FFFFFFFF"/></font><font><b/><sz val="10"/><name val="Aptos"/><color rgb="FF1F2937"/></font></fonts>
          <fills count="7"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF1F4788"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFF3F6FA"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFFFFFFF"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFE7F6EC"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFFFF4CE"/><bgColor indexed="64"/></patternFill></fill></fills>
          <borders count="2"><border/><border><left/><right/><top/><bottom style="thin"><color rgb="FFD7DEE8"/></bottom><diagonal/></border></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="11">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="3" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="4" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="14" fontId="0" fillId="3" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="14" fontId="0" fillId="4" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="165" fontId="0" fillId="3" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="165" fontId="0" fillId="4" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="0" fontId="2" fillId="5" borderId="1" xfId="0" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="3" borderId="1" xfId="0" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="2" fillId="6" borderId="1" xfId="0" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Status(string status) => status switch
    {
        "Complete" => "Tamamlandı", "NoRecord" => "Kayıt yok", "NonWorkingDay" => "Çalışma dışı gün",
        "MissingEntry" => "Giriş eksik", "MissingExit" => "Çıkış eksik", "FieldWork" => "Saha çalışması",
        "RemoteWork" => "Uzaktan çalışma", _ => status
    };
    private static string WorkLocationLabel(string location) => location switch { "Remote" => "Uzaktan", "Field" => "Saha", _ => "Ofis" };
    private static string WorkLocation(AttendanceReportRowDto row) => string.IsNullOrWhiteSpace(row.WorkLocationDetail)
        ? WorkLocationLabel(row.WorkLocation)
        : $"{WorkLocationLabel(row.WorkLocation)} - {row.WorkLocationDetail}";
    private static string ShortTime(DateTimeOffset? value) => value?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "--";
    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
    private static void Write(Stream stream, string value) => stream.Write(Ascii(value));
    private static string PdfEscape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string ToAscii(string value) => value
        .Replace('ç', 'c').Replace('Ç', 'C').Replace('ğ', 'g').Replace('Ğ', 'G').Replace('ı', 'i').Replace('İ', 'I')
        .Replace('ö', 'o').Replace('Ö', 'O').Replace('ş', 's').Replace('Ş', 'S').Replace('ü', 'u').Replace('Ü', 'U');
}

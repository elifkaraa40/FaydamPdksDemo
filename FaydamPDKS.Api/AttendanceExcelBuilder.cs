using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Api;

internal static class AttendanceExcelBuilder
{
    public static byte[] Build(AttendanceReportDto report, bool english)
    {
        var headers = english
            ? new[] { "Employee No", "Employee", "Department", "Date", "Shift", "Status", "Work Location", "Work Detail", "First Entry", "Last Exit", "Worked Minutes", "Expected Minutes", "Late Minutes", "Overtime Minutes" }
            : new[] { "Sicil No", "Personel", "Bölüm", "Tarih", "Vardiya", "Durum", "Çalışma Şekli", "Çalışma Detayı", "İlk Giriş", "Son Çıkış", "Çalışılan Dakika", "Beklenen Dakika", "Geç Dakika", "Fazla Mesai Dakika" };
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
                <cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:title>{(english ? "Attendance Report" : "Puantaj Raporu")}</dc:title><dc:creator>Faydam PDKS</dc:creator><cp:lastModifiedBy>Faydam PDKS</cp:lastModifiedBy></cp:coreProperties>
                """);
            WriteEntry(archive, "docProps/app.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"><Application>Faydam PDKS</Application></Properties>
                """);
            WriteEntry(archive, "xl/workbook.xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><bookViews><workbookView/></bookViews><sheets><sheet name="{(english ? "Attendance" : "Puantaj")}" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
                """);
            WriteEntry(archive, "xl/styles.xml", ExcelStyles);

            var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
            using var writer = XmlWriter.Create(entry.Open(), new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
            writer.WriteStartDocument(true);
            writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            writer.WriteStartElement("dimension"); writer.WriteAttributeString("ref", $"A1:N{Math.Max(5, report.Rows.Count + 4)}"); writer.WriteEndElement();
            writer.WriteStartElement("sheetViews"); writer.WriteStartElement("sheetView"); writer.WriteAttributeString("showGridLines", "0"); writer.WriteAttributeString("workbookViewId", "0");
            writer.WriteStartElement("pane"); writer.WriteAttributeString("ySplit", "4"); writer.WriteAttributeString("topLeftCell", "A5"); writer.WriteAttributeString("activePane", "bottomLeft"); writer.WriteAttributeString("state", "frozen"); writer.WriteEndElement();
            writer.WriteEndElement(); writer.WriteEndElement();
            writer.WriteStartElement("sheetFormatPr"); writer.WriteAttributeString("defaultRowHeight", "15"); writer.WriteEndElement();
            writer.WriteStartElement("cols");
            double[] widths = [14, 24, 20, 13, 23, 18, 18, 28, 18, 18, 19, 17, 14, 22];
            for (var index = 0; index < widths.Length; index++)
            {
                writer.WriteStartElement("col");
                writer.WriteAttributeString("min", (index + 1).ToString());
                writer.WriteAttributeString("max", (index + 1).ToString());
                writer.WriteAttributeString("width", widths[index].ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("customWidth", "1");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteStartElement("sheetData");
            WriteTitleRows(writer, report, english);
            WriteHeader(writer, headers, 4);
            for (var index = 0; index < report.Rows.Count; index++)
                WriteDataRow(writer, report.Rows[index], index % 2 == 0, index + 5, english);
            if (report.Rows.Count == 0)
                WriteEmptyRow(writer, english);
            writer.WriteEndElement();
            writer.WriteStartElement("mergeCells"); writer.WriteAttributeString("count", "2");
            writer.WriteStartElement("mergeCell"); writer.WriteAttributeString("ref", "A1:N1"); writer.WriteEndElement();
            writer.WriteStartElement("mergeCell"); writer.WriteAttributeString("ref", "A2:N2"); writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement("autoFilter"); writer.WriteAttributeString("ref", $"A4:N{Math.Max(5, report.Rows.Count + 4)}"); writer.WriteEndElement();
            writer.WriteStartElement("pageMargins"); writer.WriteAttributeString("left", "0.25"); writer.WriteAttributeString("right", "0.25"); writer.WriteAttributeString("top", "0.5"); writer.WriteAttributeString("bottom", "0.5"); writer.WriteAttributeString("header", "0.2"); writer.WriteAttributeString("footer", "0.2"); writer.WriteEndElement();
            writer.WriteStartElement("pageSetup"); writer.WriteAttributeString("orientation", "landscape"); writer.WriteAttributeString("fitToWidth", "1"); writer.WriteAttributeString("fitToHeight", "0"); writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        return output.ToArray();
    }

    private static void WriteTitleRows(XmlWriter writer, AttendanceReportDto report, bool english)
    {
        writer.WriteStartElement("row"); writer.WriteAttributeString("r", "1"); writer.WriteAttributeString("ht", "34"); writer.WriteAttributeString("customHeight", "1");
        WriteTextCell(writer, english ? "ATTENDANCE REPORT" : "PUANTAJ RAPORU", 11, "A1");
        writer.WriteEndElement();

        writer.WriteStartElement("row"); writer.WriteAttributeString("r", "2"); writer.WriteAttributeString("ht", "23"); writer.WriteAttributeString("customHeight", "1");
        var period = english
            ? $"Period: {report.From:dd.MM.yyyy} - {report.To:dd.MM.yyyy}  |  {report.Rows.Count} records"
            : $"Dönem: {report.From:dd.MM.yyyy} - {report.To:dd.MM.yyyy}  |  {report.Rows.Count} kayıt";
        WriteTextCell(writer, period, 12, "A2");
        writer.WriteEndElement();

        writer.WriteStartElement("row"); writer.WriteAttributeString("r", "3"); writer.WriteAttributeString("ht", "8"); writer.WriteAttributeString("customHeight", "1"); writer.WriteEndElement();
    }

    private static void WriteHeader(XmlWriter writer, IReadOnlyList<string> headers, int rowNumber)
    {
        writer.WriteStartElement("row"); writer.WriteAttributeString("r", rowNumber.ToString()); writer.WriteAttributeString("ht", "30"); writer.WriteAttributeString("customHeight", "1");
        for (var index = 0; index < headers.Count; index++)
            WriteTextCell(writer, headers[index], 1, $"{ColumnName(index)}{rowNumber}");
        writer.WriteEndElement();
    }

    private static void WriteEmptyRow(XmlWriter writer, bool english)
    {
        writer.WriteStartElement("row"); writer.WriteAttributeString("r", "5"); writer.WriteAttributeString("ht", "26"); writer.WriteAttributeString("customHeight", "1");
        WriteTextCell(writer, english ? "No records found for the selected period." : "Seçilen dönemde kayıt bulunamadı.", 2, "A5");
        writer.WriteEndElement();
    }

    private static void WriteDataRow(XmlWriter writer, AttendanceReportRowDto row, bool alternate, int rowNumber, bool english)
    {
        var textStyle = alternate ? 2 : 3;
        var dateStyle = alternate ? 4 : 5;
        var numberStyle = alternate ? 6 : 7;
        var statusStyle = row.Status == "Complete" ? 8 : row.Status is "NoRecord" or "NonWorkingDay" ? 9 : 10;
        writer.WriteStartElement("row"); writer.WriteAttributeString("r", rowNumber.ToString()); writer.WriteAttributeString("ht", "22"); writer.WriteAttributeString("customHeight", "1");
        WriteTextCell(writer, row.EmployeeNumber, textStyle, $"A{rowNumber}");
        WriteTextCell(writer, row.EmployeeName, textStyle, $"B{rowNumber}");
        WriteTextCell(writer, row.Department, textStyle, $"C{rowNumber}");
        WriteNumberCell(writer, row.WorkDate.ToDateTime(TimeOnly.MinValue).ToOADate(), dateStyle, $"D{rowNumber}");
        WriteTextCell(writer, row.ShiftName, textStyle, $"E{rowNumber}");
        WriteTextCell(writer, Status(row.Status, english), statusStyle, $"F{rowNumber}");
        WriteTextCell(writer, WorkLocation(row.WorkLocation, english), textStyle, $"G{rowNumber}");
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
        writer.WriteStartElement("c"); writer.WriteAttributeString("r", reference); writer.WriteAttributeString("s", style.ToString());
        writer.WriteElementString("v", value.ToString(CultureInfo.InvariantCulture)); writer.WriteEndElement();
    }

    private static string ColumnName(int index) => ((char)('A' + index)).ToString();

    private static string WorkLocation(string value, bool english) => english
        ? value switch { "Remote" => "Remote", "Field" => "Field", _ => "Office" }
        : value switch { "Remote" => "Uzaktan", "Field" => "Saha", _ => "Ofis" };

    private static string Status(string status, bool english) => english
        ? status switch { "Complete" => "Completed", "NoRecord" => "No record", "NonWorkingDay" => "Non-working day", "MissingEntry" => "Missing entry", "MissingExit" => "Missing exit", "FieldWork" => "Field work", "RemoteWork" => "Remote work", _ => status }
        : status switch { "Complete" => "Tamamlandı", "NoRecord" => "Kayıt yok", "NonWorkingDay" => "Çalışma dışı gün", "MissingEntry" => "Giriş eksik", "MissingExit" => "Çıkış eksik", "FieldWork" => "Saha çalışması", "RemoteWork" => "Uzaktan çalışma", _ => status };

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private const string ExcelStyles = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <numFmts count="1"><numFmt numFmtId="165" formatCode="#,##0 &quot;dk&quot;"/></numFmts>
          <fonts count="5"><font><sz val="10"/><name val="Aptos"/><color rgb="FF1F2937"/></font><font><b/><sz val="10"/><name val="Aptos Display"/><color rgb="FFFFFFFF"/></font><font><b/><sz val="10"/><name val="Aptos"/><color rgb="FF1F2937"/></font><font><b/><sz val="18"/><name val="Aptos Display"/><color rgb="FFFFFFFF"/></font><font><sz val="10"/><name val="Aptos"/><color rgb="FFD7E5F5"/></font></fonts>
          <fills count="7"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF1F4788"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFF3F6FA"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFFFFFFF"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFE7F6EC"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFFFF4CE"/><bgColor indexed="64"/></patternFill></fill></fills>
          <borders count="2"><border/><border><left/><right/><top/><bottom style="thin"><color rgb="FFD7DEE8"/></bottom><diagonal/></border></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="13">
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
            <xf numFmtId="0" fontId="3" fillId="2" borderId="0" xfId="0" applyAlignment="1"><alignment horizontal="left" vertical="center"/></xf>
            <xf numFmtId="0" fontId="4" fillId="2" borderId="0" xfId="0" applyAlignment="1"><alignment horizontal="left" vertical="center"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;
}

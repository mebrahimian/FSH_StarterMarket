using System.Globalization;
using System.Text.Json;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

internal static class CodalCellReader
{
    public static CodalCellResult? FindCellValue(
        string html,
        int metaTableCode,
        int columnSequence,
        int? occurrence = null)
    {
        var datasourceStart = html.IndexOf(
            "var datasource",
            StringComparison.OrdinalIgnoreCase);

        if (datasourceStart < 0)
            return null;

        var jsonStart = html.IndexOf(
            '{',
            datasourceStart);

        if (jsonStart < 0)
            return null;

        var jsonEnd = FindJsonObjectEnd(
            html,
            jsonStart);

        if (jsonEnd < 0)
            return null;

        var json = html.Substring(
            jsonStart,
            jsonEnd - jsonStart + 1);


        using var document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;
        // پیدا کردن tracingNo
        long tracingNo = root
            .GetProperty("tracingNo")
            .GetInt64();
        // پیداکردن ReportingType : نوع شرکتها 1000000:تولیدی و 1000001: ساختمانی و...
        int reportingTypeCode = root
            .GetProperty("sheets")[0]
            .GetProperty("code")
            .GetInt32();
        string? periodETD = root
            .GetProperty("periodEndToDate")
            .GetString();
        string? yearETD = root
            .GetProperty("yearEndToDate")
            .GetString();


        // پیدا کردن cell مورد نظر

        var cells = document.RootElement
           .GetProperty("sheets")
           .EnumerateArray()
           .SelectMany(sheet =>
                       sheet.GetProperty("tables")
                            .EnumerateArray())
           .Where(table =>
                  table.TryGetProperty("code", out var tableCode) &&
                  tableCode.GetInt32() == metaTableCode)
           .SelectMany(table =>
                       table.GetProperty("cells")
           .EnumerateArray());


        var matchedCells = cells
            .Where(cell =>
                cell.TryGetProperty(
                    "columnSequence",
                    out var column) &&
                column.GetInt32() == columnSequence)
            .OrderBy(cell =>
                cell.GetProperty("rowSequence")
                    .GetInt32())
            .ToList();


        if (matchedCells.Count == 0)
            return null;


        JsonElement selectedCell;

        if (!occurrence.HasValue || occurrence.Value == 0)
        {
            // آخرین مورد
            selectedCell = matchedCells[^1];
        }
        else
        {
            // n امین مورد
            var index = occurrence.Value - 1;

            if (index >= matchedCells.Count)
                return null;

            selectedCell = matchedCells[index];
        }

        return new CodalCellResult
                         (
                           Value: GetString(selectedCell, "value"),
                           Formula: GetString(selectedCell, "formula"),
                           PeriodEndToDate: periodETD,
                           YearEndToDate: yearETD,
                           Address: GetString(selectedCell, "address"),
                           RowSequence: GetInt(selectedCell, "rowSequence"),
                           ReportingTypeCode: reportingTypeCode
                         );
    }

    public static IReadOnlyList<CodalTableRow> ReadTableRows(
    string html,
    int metaTableCode)
    {
        using JsonDocument? document = ParseDatasource(html);

        if (document is null)
            return [];

        return BuildTableRows(document.RootElement,
            metaTableCode);
    }
    public static CodalTableData? ReadTableData(
    string html,
    int metaTableCode)
    {
        using JsonDocument? document =
            ParseDatasource(html);

        if (document is null)
            return null;

        JsonElement root =
            document.RootElement;

        CodalReportHeaderData header = ReadReportHeader(html);

        CodalDatasourceMetadata? metadata =
            BuildMetadata(
                root,
                header)
                .FirstOrDefault(x =>
                    x.MetaTableCode == metaTableCode);

        if (metadata is null)
            return null;

        List<CodalTableRow> rows =
            BuildTableRows(
                root,
                metaTableCode);

        return new CodalTableData(
            metadata,
            rows);
    }
    private static List<CodalTableRow> BuildTableRows(
    JsonElement root,
    int metaTableCode)
    {
        var rows =
            new Dictionary<int, Dictionary<int, string?>>();

        foreach (JsonElement sheet in root
                     .GetProperty("sheets")
                     .EnumerateArray())
        {
            foreach (JsonElement table in sheet
                         .GetProperty("tables")
                         .EnumerateArray())
            {
                if (!table.TryGetProperty(
                        "code",
                        out JsonElement tableCode) ||
                    tableCode.GetInt32() != metaTableCode)
                {
                    continue;
                }

                foreach (JsonElement cell in table
                             .GetProperty("cells")
                             .EnumerateArray())
                {
                    if (!cell.TryGetProperty(
                            "rowSequence",
                            out JsonElement rowElement) ||
                        !cell.TryGetProperty(
                            "columnSequence",
                            out JsonElement columnElement))
                    {
                        continue;
                    }

                    int rowSequence =
                        rowElement.GetInt32();

                    int columnSequence =
                        columnElement.GetInt32();

                    string? value = null;

                    if (cell.TryGetProperty(
                            "value",
                            out JsonElement valueElement))
                    {
                        value = valueElement.ValueKind switch
                        {
                            JsonValueKind.String =>
                                valueElement.GetString(),

                            JsonValueKind.Number =>
                                valueElement.GetRawText(),

                            JsonValueKind.Null =>
                                null,

                            _ =>
                                valueElement.ToString()
                        };
                    }

                    if (!rows.TryGetValue(
                            rowSequence,
                            out Dictionary<int, string?>? row))
                    {
                        row = [];
                        rows[rowSequence] = row;
                    }

                    row[columnSequence] = value;
                }
            }
        }

        return rows
            .OrderBy(x => x.Key)
            .Select(x =>
                new CodalTableRow(
                    x.Key,
                    x.Value))
            .ToList();
    }
    private static CodalReportHeaderData ReadReportHeader(
    string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        static string? ReadSpanText(
            string source,
            params string[] elementIds)
        {
            foreach (string elementId in elementIds)
            {
                int idIndex = source.IndexOf(
                    $"id=\"{elementId}\"",
                    StringComparison.OrdinalIgnoreCase);

                if (idIndex < 0)
                    continue;

                int valueStart = source.IndexOf(
                    '>',
                    idIndex);

                if (valueStart < 0)
                    continue;

                int valueEnd = source.IndexOf(
                    "</span>",
                    valueStart,
                    StringComparison.OrdinalIgnoreCase);

                if (valueEnd < 0)
                    continue;

                string innerHtml = source.Substring(
                    valueStart + 1,
                    valueEnd - valueStart - 1);

                // بعضی قالب‌های قدیمی کدال مقدار را داخل
                // <font>...</font> قرار می‌دهند.
                char[] buffer = new char[innerHtml.Length];
                int writeIndex = 0;
                bool insideTag = false;

                foreach (char character in innerHtml)
                {
                    if (character == '<')
                    {
                        insideTag = true;
                        continue;
                    }

                    if (character == '>')
                    {
                        insideTag = false;
                        continue;
                    }

                    if (!insideTag)
                    {
                        buffer[writeIndex++] = character;
                    }
                }

                string value =
                    System.Net.WebUtility
                        .HtmlDecode(
                            new string(buffer[..writeIndex]))
                        .Trim();

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        static decimal? ReadDecimal(
            string source,
            params string[] elementIds)
        {
            string? value = ReadSpanText(
                source,
                elementIds);

            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalizedValue = value
                .Replace(
                    ",",
                    string.Empty,
                    StringComparison.Ordinal)
                .Replace(
                    "٬",
                    string.Empty,
                    StringComparison.Ordinal);

            return decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal parsedValue)
                    ? parsedValue
                    : null;
        }

        string? reportSymbol = ReadSpanText(
            html,
            "ctl00_txbSymbol",
            "ctl00_lblDisplaySymbol");

        string? reportCompanyName = ReadSpanText(
            html,
            "ctl00_txbCompanyName");

        decimal? registeredCapital = ReadDecimal(
            html,
            "ctl00_lblListedCapital");

        decimal? unauthorizedCapital = ReadDecimal(
            html,
            "ctl00_txbUnauthorizedCapital");

        return new CodalReportHeaderData(
            ReportSymbol: reportSymbol,
            ReportCompanyName: reportCompanyName,
            RegisteredCapital: registeredCapital,
            UnauthorizedCapital: unauthorizedCapital);
    }
    private static List<CodalDatasourceMetadata> BuildMetadata(
    JsonElement root,
    CodalReportHeaderData header)
    {
        long tracingNo =
            root.GetProperty("tracingNo")
                .GetInt64();

        string? periodEndToDate =
            GetString(root, "periodEndToDate");

        string? yearEndToDate =
            GetString(root, "yearEndToDate");

        string? period =
            GetRawValue(root, "period");

        string? type =
            GetRawValue(root, "type");

        var result =
            new List<CodalDatasourceMetadata>();

        foreach (JsonElement sheet in root
                     .GetProperty("sheets")
                     .EnumerateArray())
        {
            int sheetCode =
                GetInt(sheet, "code");

            foreach (JsonElement table in sheet
                         .GetProperty("tables")
                         .EnumerateArray())
            {
                result.Add(
                    new CodalDatasourceMetadata(
                        TracingNo: tracingNo,
                        PeriodEndToDate: periodEndToDate,
                        YearEndToDate: yearEndToDate,
                        Period: period,
                        Type: type,
                        SheetCode: GetInt(table, "sheetCode"),
                        MetaTableId: GetInt(table, "metaTableId"),
                        MetaTableCode: GetInt(table, "code"),
                        TitleFa: GetString(table, "title_Fa"),
                        TitleEn: GetString(table, "title_En"),
                        ReportSymbol: header.ReportSymbol,
                        ReportCompanyName: header.ReportCompanyName,
                        RegisteredCapital: header.RegisteredCapital,
                        UnauthorizedCapital: header.UnauthorizedCapital));
            }
        }

        return result;
    }
    public static List<CodalDatasourceMetadata> ReadMetadata(
    string html)
    {
        using JsonDocument? document = ParseDatasource(html);

        if (document is null)
            return [];

        CodalReportHeaderData header =
            ReadReportHeader(html);

        return BuildMetadata(
            document.RootElement,
            header);
    }
    /*
     SumColumnValues
             → جدول ردیف جمع دارد
             → ولی مقدار ردیف جمع قابل استفاده نیست
             → همه ردیف‌ها به جز آخرین ردیف را جمع می‌کند

     SumColumnValuesWithoutTotalRow
             → جدول اصلاً ردیف جمع ندارد
             → تمام ردیف‌های عددی ستون را جمع می‌کند
    */
    public static CodalCellResult? SumColumnValues(string html, int metaTableCode, int columnSequence)
    {
        int datasourceStart = html.IndexOf(
            "var datasource",
            StringComparison.OrdinalIgnoreCase);

        if (datasourceStart < 0)
            return null;

        int jsonStart = html.IndexOf(
            '{',
            datasourceStart);

        if (jsonStart < 0)
            return null;

        int jsonEnd = FindJsonObjectEnd(
            html,
            jsonStart);

        if (jsonEnd < 0)
            return null;

        string json = html.Substring(
            jsonStart,
            jsonEnd - jsonStart + 1);

        using var document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;

        int reportingTypeCode = root
            .GetProperty("sheets")[0]
            .GetProperty("code")
            .GetInt32();
        string? periodETD = root
            .GetProperty("periodEndToDate")
            .GetString();
        string? yearETD = root
            .GetProperty("yearEndToDate")
            .GetString();
        var matchedCells = root
            .GetProperty("sheets")
            .EnumerateArray()
            .SelectMany(sheet =>
                sheet.GetProperty("tables")
                    .EnumerateArray())
            .Where(table =>
                table.TryGetProperty(
                    "code",
                    out JsonElement tableCode) &&
                tableCode.GetInt32() == metaTableCode)
            .SelectMany(table =>
                table.GetProperty("cells")
                    .EnumerateArray())
            .Where(cell =>
                cell.TryGetProperty(
                    "columnSequence",
                    out JsonElement column) &&
                column.GetInt32() == columnSequence)
            .OrderBy(cell =>
                cell.GetProperty("rowSequence")
                    .GetInt32())
            .ToList();

        // حداقل یک ردیف جزئی و یک ردیف جمع کل لازم است.
        if (matchedCells.Count <= 1)
            return null;

        JsonElement totalCell = matchedCells[^1];

        decimal sum = 0m;
        bool hasValue = false;

        // آخرین ردیف جمع کل است و دوباره جمع نمی‌شود.
        foreach (JsonElement cell in matchedCells.Take(
                     matchedCells.Count - 1))
        {
            if (!cell.TryGetProperty(
                    "value",
                    out JsonElement valueElement))
            {
                continue;
            }

            decimal numericValue;

            if (valueElement.ValueKind == JsonValueKind.String)
            {
                string? rawValue = valueElement.GetString();

                if (!decimal.TryParse(
                        rawValue,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out numericValue))
                {
                    continue;
                }
            }
            else if (valueElement.ValueKind == JsonValueKind.Number)
            {
                if (!valueElement.TryGetDecimal(
                        out numericValue))
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            sum += numericValue;
            hasValue = true;
        }

        if (!hasValue)
            return null;

        return new CodalCellResult(
            Value: sum.ToString(
                CultureInfo.InvariantCulture),
            Formula: GetString(
                totalCell,
                "formula"),
            PeriodEndToDate: periodETD,
            YearEndToDate: yearETD,
            Address: GetString(
                totalCell,
                "address"),
            RowSequence: GetInt(
                totalCell,
                "rowSequence"),
            ReportingTypeCode: reportingTypeCode);
    }

    public static CodalCellResult? SumColumnValuesWithoutTotalRow(
    string html,
    int metaTableCode,
    int columnSequence)
    {
        int datasourceStart = html.IndexOf(
            "var datasource",
            StringComparison.OrdinalIgnoreCase);

        if (datasourceStart < 0)
            return null;

        int jsonStart = html.IndexOf(
            '{',
            datasourceStart);

        if (jsonStart < 0)
            return null;

        int jsonEnd = FindJsonObjectEnd(
            html,
            jsonStart);

        if (jsonEnd < 0)
            return null;

        string json = html.Substring(
            jsonStart,
            jsonEnd - jsonStart + 1);

        using var document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;

        int reportingTypeCode = root
            .GetProperty("sheets")[0]
            .GetProperty("code")
            .GetInt32();

        string? periodETD = root
            .GetProperty("periodEndToDate")
            .GetString();

        string? yearETD = root
            .GetProperty("yearEndToDate")
            .GetString();

        var matchedCells = root
            .GetProperty("sheets")
            .EnumerateArray()
            .SelectMany(sheet =>
                sheet.GetProperty("tables")
                    .EnumerateArray())
            .Where(table =>
                table.TryGetProperty(
                    "code",
                    out JsonElement tableCode) &&
                tableCode.GetInt32() == metaTableCode)
            .SelectMany(table =>
                table.GetProperty("cells")
                    .EnumerateArray())
            .Where(cell =>
                cell.TryGetProperty(
                    "columnSequence",
                    out JsonElement column) &&
                column.GetInt32() == columnSequence)
            .OrderBy(cell =>
                cell.GetProperty("rowSequence")
                    .GetInt32())
            .ToList();

        if (matchedCells.Count == 0)
            return null;

        decimal sum = 0m;
        bool hasValue = false;

        // این جدول ردیف جمع کل ندارد،
        // بنابراین تمام ردیف‌های عددی ستون جمع می‌شوند.
        foreach (JsonElement cell in matchedCells)
        {
            if (!cell.TryGetProperty(
                    "value",
                    out JsonElement valueElement))
            {
                continue;
            }

            decimal numericValue;

            if (valueElement.ValueKind == JsonValueKind.String)
            {
                string? rawValue = valueElement.GetString();

                if (!decimal.TryParse(
                        rawValue,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out numericValue))
                {
                    continue;
                }
            }
            else if (valueElement.ValueKind == JsonValueKind.Number)
            {
                if (!valueElement.TryGetDecimal(
                        out numericValue))
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            sum += numericValue;
            hasValue = true;
        }

        if (!hasValue)
            return null;

        JsonElement metadataCell = matchedCells[^1];

        return new CodalCellResult(
            Value: sum.ToString(
                CultureInfo.InvariantCulture),
            Formula: GetString(
                metadataCell,
                "formula"),
            PeriodEndToDate: periodETD,
            YearEndToDate: yearETD,
            Address: GetString(
                metadataCell,
                "address"),
            RowSequence: GetInt(
                metadataCell,
                "rowSequence"),
            ReportingTypeCode: reportingTypeCode);
    }

    private static string? GetRawValue(
    JsonElement element,
    string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out JsonElement property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String =>
                property.GetString(),

            JsonValueKind.Number =>
                property.GetRawText(),

            JsonValueKind.True =>
                bool.TrueString,

            JsonValueKind.False =>
                bool.FalseString,

            JsonValueKind.Null =>
                null,

            _ =>
                property.ToString()
        };
    }

    private static JsonDocument? ParseDatasource(
    string html)
    {
        int datasourceStart = html.IndexOf(
            "var datasource",
            StringComparison.OrdinalIgnoreCase);

        if (datasourceStart < 0)
            return null;

        int jsonStart = html.IndexOf(
            '{',
            datasourceStart);

        if (jsonStart < 0)
            return null;

        int jsonEnd = FindJsonObjectEnd(
            html,
            jsonStart);

        if (jsonEnd < 0)
            return null;

        string json = html.Substring(
            jsonStart,
            jsonEnd - jsonStart + 1);

        return JsonDocument.Parse(json);
    }
    private static int FindJsonObjectEnd(
        string text,
        int start)
    {
        var depth = 0;
        var inString = false;
        var escape = false;


        for (var i = start; i < text.Length; i++)
        {
            var ch = text[i];


            if (escape)
            {
                escape = false;
                continue;
            }


            if (ch == '\\' && inString)
            {
                escape = true;
                continue;
            }


            if (ch == '"')
            {
                inString = !inString;
                continue;
            }


            if (inString)
                continue;


            if (ch == '{')
                depth++;

            else if (ch == '}')
            {
                depth--;

                if (depth == 0)
                    return i;
            }
        }


        return -1;
    }
    private static string? GetString(
    JsonElement element,
    string property)
    {
        return element.TryGetProperty(
            property,
            out var value)
            ? value.GetString()
            : null;
    }

    private static int GetInt(
        JsonElement element,
        string property)
    {
        return element.TryGetProperty(
            property,
            out var value)
            ? value.GetInt32()
            : 0;
    }
}
internal sealed record CodalDatasourceMetadata(
    long TracingNo,
    string? PeriodEndToDate,
    string? YearEndToDate,
    string? Period,
    string? Type,
    int SheetCode,
    int MetaTableId,
    int MetaTableCode,
    string? TitleFa,
    string? TitleEn,
    string? ReportSymbol,
    string? ReportCompanyName,
    decimal? RegisteredCapital,
    decimal? UnauthorizedCapital);

internal sealed record CodalTableRow(
    int RowSequence,
    IReadOnlyDictionary<int, string?> Values);
internal sealed record CodalTableData(
    CodalDatasourceMetadata Metadata,
    IReadOnlyList<CodalTableRow> Rows);
internal sealed record CodalReportHeaderData(
    string? ReportSymbol,
    string? ReportCompanyName,
    decimal? RegisteredCapital,
    decimal? UnauthorizedCapital);
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


    /*
     SumColumnValues
             → جدول ردیف جمع دارد
             → ولی مقدار ردیف جمع قابل استفاده نیست
             → همه ردیف‌ها به جز آخرین ردیف را جمع می‌کند

     SumColumnValuesWithoutTotalRow
             → جدول اصلاً ردیف جمع ندارد
             → تمام ردیف‌های عددی ستون را جمع می‌کند
    */
    public static IReadOnlyList<CodalTableRow> ReadTableRows(
    string html,
    int metaTableCode)
    {
        int datasourceStart = html.IndexOf(
            "var datasource",
            StringComparison.OrdinalIgnoreCase);

        if (datasourceStart < 0)
        {
            return [];
        }

        int jsonStart = html.IndexOf(
            '{',
            datasourceStart);

        if (jsonStart < 0)
        {
            return [];
        }

        int jsonEnd = FindJsonObjectEnd(
            html,
            jsonStart);

        if (jsonEnd < 0)
        {
            return [];
        }

        string json = html.Substring(
            jsonStart,
            jsonEnd - jsonStart + 1);

        using var document = JsonDocument.Parse(json);

        var rows =
            new Dictionary<int, Dictionary<int, string?>>();

        foreach (JsonElement sheet in document.RootElement
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
    public static CodalDatasourceMetadata? ReadMetadata(
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

        using var document =
            JsonDocument.Parse(json);

        JsonElement root =
            document.RootElement;

        long tracingNo =
            root.TryGetProperty(
                "tracingNo",
                out JsonElement tracingNoElement)
                ? tracingNoElement.GetInt64()
                : 0;

        string? periodEndToDate =
            root.TryGetProperty(
                "periodEndToDate",
                out JsonElement periodElement)
                ? periodElement.GetString()
                : null;

        string? yearEndToDate =
            root.TryGetProperty(
                "yearEndToDate",
                out JsonElement yearElement)
                ? yearElement.GetString()
                : null;

        return new CodalDatasourceMetadata(
            TracingNo: tracingNo,
            PeriodEndToDate: periodEndToDate,
            YearEndToDate: yearEndToDate);
    }
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
    string? YearEndToDate);

internal sealed record CodalTableRow(
    int RowSequence,
    IReadOnlyDictionary<int, string?> Values);
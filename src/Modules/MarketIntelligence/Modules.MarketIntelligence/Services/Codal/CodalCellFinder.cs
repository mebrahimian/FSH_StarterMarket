using System.Text.Json;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

internal static class CodalCellFinder
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
                           PeriodEndToDate: GetString(selectedCell, "periodEndToDate"),
                           YearEndToDate: GetString(selectedCell, "yearEndToDate"),
                           Address: GetString(selectedCell, "address"),
                           RowSequence: GetInt(selectedCell, "rowSequence"),
                           ReportingTypeCode: reportingTypeCode
                         );
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
using System.Text.Json;
using static FSH.Modules.MarketIntelligence.Services.Codal.CodalTableDefinitions;
namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class MonthlySalesParser : IMonthlySalesParser
{
    public Task<MonthlySalesParseResult?> ParseAsync(
        string datasourceJson,
        CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(datasourceJson);

        var root = document.RootElement;

        var sheets = root.GetProperty("sheets");

        foreach (var sheet in sheets.EnumerateArray())
        {
            var tables = sheet.GetProperty("tables");

            foreach (var table in tables.EnumerateArray())
            {
                var metaTableId = table.GetProperty("metaTableId").GetInt32();
                var metaTableCode = table.GetProperty("code").GetInt32();

                if (IsManufacturingMonthlySalesTable(
         metaTableId,
         metaTableCode))
                {
                    return Task.FromResult<MonthlySalesParseResult?>(null);
                }
            }
        }

        return Task.FromResult<MonthlySalesParseResult?>(null);
    }
}
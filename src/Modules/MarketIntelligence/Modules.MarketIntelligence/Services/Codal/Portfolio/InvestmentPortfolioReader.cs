using System.Text.Json;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using Microsoft.Extensions.Options;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

public sealed class InvestmentPortfolioReader(
    HttpClient httpClient,
    IOptions<CodalOptions> options)
{
    private const int ListedPortfolioMetaTableId = 3475;
    private const int ListedPortfolioMetaTableCode = 3475;

    public async Task<string?> ReadAsync(
        string disclosureUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            disclosureUrl);

        Uri uri = Uri.TryCreate(
            disclosureUrl,
            UriKind.Absolute,
            out Uri? absoluteUri)
                ? absoluteUri
                : new Uri(
                    new Uri(options.Value.BaseUrl),
                    disclosureUrl);

        string html = await httpClient
            .GetStringAsync(
                uri,
                cancellationToken)
            .ConfigureAwait(false);

        int datasourceStart = html.IndexOf(
            "var datasource",
            StringComparison.OrdinalIgnoreCase);

        if (datasourceStart < 0)
        {
            return null;
        }

        int jsonStart = html.IndexOf(
            '{',
            datasourceStart);

        if (jsonStart < 0)
        {
            return null;
        }

        int jsonEnd = FindJsonObjectEnd(
            html,
            jsonStart);

        if (jsonEnd < 0)
        {
            return null;
        }

        string json = html.Substring(
            jsonStart,
            jsonEnd - jsonStart + 1);

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root =
            document.RootElement;

        if (!root.TryGetProperty("sheets", out JsonElement sheets) ||
                  sheets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        for (int sheetIndex = 0;
     sheetIndex < sheets.GetArrayLength();
     sheetIndex++)
        {
            JsonElement sheet = sheets[sheetIndex];
            if (!sheet.TryGetProperty("tables", out JsonElement tables) ||
                    tables.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            for (int tableIndex = 0; tableIndex < tables.GetArrayLength(); tableIndex++)
            {
                JsonElement table = tables[tableIndex];

                if (IsListedPortfolioTable(table))
                {
                    return table.GetRawText();
                }
            }
        }

        return null;
    }

    private static bool IsListedPortfolioTable(
        JsonElement table)
    {
        bool codeMatches = false;

        if (table.TryGetProperty("metaTableCode", out JsonElement metaTableCode) &&
             metaTableCode.TryGetInt32(out int explicitCode))
        {
            codeMatches = explicitCode == ListedPortfolioMetaTableCode;
        }
        else if (table.TryGetProperty("code", out JsonElement code) &&
                    code.TryGetInt32(out int tableCode))
        {
            codeMatches = tableCode == ListedPortfolioMetaTableCode;
        }

        if (!codeMatches)
        {
            return false;
        }

        if (table.TryGetProperty("metaTableId", out JsonElement metaTableId) &&
            metaTableId.TryGetInt32(out int tableId))
        {
            return tableId == ListedPortfolioMetaTableId;
        }

        return true;
    }

    private static int FindJsonObjectEnd(string text, int start)
    {
        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = start; i < text.Length; i++)
        {
            char ch = text[i];

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
            {
                continue;
            }

            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
namespace FSH.Modules.MarketIntelligence.Services.Codal;

internal static class PortfolioSheetResolver
{
    public static IReadOnlyList<PortfolioSheet> GetSheets(string disclosureUrl)
    {
        return
        [
            new PortfolioSheet(
                SheetId: 4,
                MetaTableCode: 1470,
                Url: BuildSheetUrl(disclosureUrl, 4),
                IsListed: true),

            new PortfolioSheet(
                SheetId: 5,
                MetaTableCode: 1471,
                Url: BuildSheetUrl(disclosureUrl, 5),
                IsListed: false)
        ];
    }

    private static string BuildSheetUrl(string disclosureUrl, int sheetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(disclosureUrl);

        int sheetIdIndex = disclosureUrl.IndexOf(
            "sheetId=",
            StringComparison.OrdinalIgnoreCase);

        if (sheetIdIndex < 0)
        {
            char separator = disclosureUrl.Contains(
                '?',
                StringComparison.Ordinal)
                ? '&'
                : '?';

            return $"{disclosureUrl}{separator}sheetId={sheetId}";
        }

        int valueStart = sheetIdIndex + "sheetId=".Length;
        int valueEnd = disclosureUrl.IndexOf('&', valueStart);

        if (valueEnd < 0)
        {
            return string.Concat(
                disclosureUrl.AsSpan(0, valueStart),
                sheetId.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
        }

        return string.Concat(
            disclosureUrl.AsSpan(0, valueStart),
            sheetId.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            disclosureUrl.AsSpan(valueEnd));
    }
}

internal sealed record PortfolioSheet(
    int SheetId,
    int MetaTableCode,
    string Url,
    bool IsListed);
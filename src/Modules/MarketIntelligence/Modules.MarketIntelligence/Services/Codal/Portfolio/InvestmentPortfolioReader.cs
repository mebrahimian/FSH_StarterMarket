using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

public sealed class InvestmentPortfolioReader(
    CodalReportHtmlDownloader htmlDownloader)
{
    internal async Task<IReadOnlyList<InvestmentPortfolioSheetData>> ReadAsync(
        string disclosureUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            disclosureUrl);

        IReadOnlyList<PortfolioSheet> sheets = PortfolioSheetResolver.GetSheets(disclosureUrl);

        var result =
            new List<InvestmentPortfolioSheetData>(sheets.Count);

        foreach (PortfolioSheet sheet in sheets)
        {
            string html = await htmlDownloader.DownloadAsync(
                sheet.Url,
                cancellationToken);
            CodalTableData? tableData = CodalCellReader.ReadTableData(html, sheet.MetaTableCode);
            if (tableData is null)
            {
                continue;
            }
            result.Add(
                new InvestmentPortfolioSheetData(
                     SheetId: sheet.SheetId,
                     MetaTableCode: sheet.MetaTableCode,
                     IsListed: sheet.IsListed,
                     PeriodEndDate: tableData.Metadata.PeriodEndToDate,
                     Metadata: tableData.Metadata,
                     Rows: tableData.Rows));
        }
        return result;
    }    
}
internal sealed record InvestmentPortfolioSheetData(
    int SheetId,
    int MetaTableCode,
    bool IsListed,
    string? PeriodEndDate,
    CodalDatasourceMetadata Metadata,
    IReadOnlyList<CodalTableRow> Rows);
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static FSH.Framework.BuildingBlocks.Shared.Globalization.PersianTextNormalizer;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

public sealed class PortfolioHistoryRebuildService(
    MarketIntelligenceDbContext dbContext,
    IEnumerable<ICodalDisclosureProcessor> processors,
    ILogger<PortfolioHistoryRebuildService> logger)
{
    public async Task<PortfolioHistoryRebuildCompany[]> BuildUniverseAsync(
    CancellationToken cancellationToken = default)
    {
        List<string> metadataSymbols =
            await dbContext.InvestmentPortfolioReportMetadata
                .AsNoTracking()
                .Join(
                    dbContext.Disclosures,
                    metadata => metadata.DisclosureId,
                    disclosure => disclosure.Id,
                    (_, disclosure) => disclosure.Symbol)
                .Where(symbol =>
                    !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol!)
                .Distinct()
                .ToListAsync(cancellationToken);

        List<int> parentCompanyIds =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .Select(position => position.ParentCompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

        List<string> positionSymbols =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(company =>
                    parentCompanyIds.Contains(company.CompanyId) &&
                    !string.IsNullOrWhiteSpace(company.Symbol))
                .Select(company => company.Symbol!)
                .Distinct()
                .ToListAsync(cancellationToken);

        string[] symbolKeys =
            metadataSymbols
                .Concat(positionSymbols)
                .Select(NormalizeForMatch)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(symbol => symbol, StringComparer.Ordinal)
                .ToArray();

        var companyRows =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(company =>
                    company.IsListed &&
                    !string.IsNullOrWhiteSpace(company.FSortSymbol))
                .Select(company => new
                {
                    company.CompanyId,
                    company.FSortSymbol
                })
                .ToListAsync(cancellationToken);

        Dictionary<string, int[]> companyIdsBySymbol =
            companyRows
                .GroupBy(company => company.FSortSymbol!)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(company => company.CompanyId)
                        .Distinct()
                        .ToArray(),
                    StringComparer.Ordinal);

        var result =
            new List<PortfolioHistoryRebuildCompany>();

        foreach (string symbolKey in symbolKeys)
        {
            if (!companyIdsBySymbol.TryGetValue(
                    symbolKey,
                    out int[]? companyIds) ||
                companyIds.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Portfolio rebuild universe could not resolve '{symbolKey}' to exactly one CompanyId.");
            }

            result.Add(
                new PortfolioHistoryRebuildCompany(
                    companyIds[0],
                    symbolKey));
        }

        return result.ToArray();
    }

    public async Task RebuildAsync(PortfolioHistoryRebuildCompany[] companies, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(companies);

        if (companies.Length == 0)
        {
            throw new InvalidOperationException(
                "Portfolio rebuild aborted: portfolio universe is empty.");
        }

        InvestmentPortfolioProcessor portfolioProcessor =
            processors
                .OfType<InvestmentPortfolioProcessor>()
                .Single();

        HashSet<string> symbolKeys =
            companies
                .Select(company => company.SymbolKey)
                .ToHashSet(StringComparer.Ordinal);

        List<Disclosure> candidateRows =
            await dbContext.Disclosures
                .AsNoTracking()
                .Where(disclosure =>
                    disclosure.Rt == 2 &&
                    (disclosure.Let == 58 ||
                     disclosure.Let == 6) &&
                    !string.IsNullOrWhiteSpace(disclosure.Symbol))
                .ToListAsync(cancellationToken);

        Dictionary<string, List<Disclosure>> disclosuresBySymbol =
            candidateRows
                .Where(disclosure =>
                    symbolKeys.Contains(
                        NormalizeForMatch(disclosure.Symbol!)))
                .GroupBy(disclosure =>
                    NormalizeForMatch(disclosure.Symbol!))
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(disclosure =>
                            disclosure.PublishDateTime)
                        .ThenBy(disclosure =>
                            disclosure.TracingNo)
                        .ToList(),
                    StringComparer.Ordinal);

        int processedCompanyCount = 0;
        int processedDisclosureCount = 0;

        foreach (PortfolioHistoryRebuildCompany company in companies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            disclosuresBySymbol.TryGetValue(
                company.SymbolKey,
                out List<Disclosure>? companyDisclosures);

            companyDisclosures ??= [];

            int missingUrlCount =
                companyDisclosures.Count(disclosure =>
                    string.IsNullOrWhiteSpace(disclosure.Url));

            if (missingUrlCount > 0)
            {
                throw new InvalidOperationException(
                    $"Portfolio rebuild aborted for CompanyId={company.CompanyId}: " +
                    $"{missingUrlCount} disclosures have no URL.");
            }

            Guid[] disclosureIds =
                companyDisclosures
                    .Select(disclosure => disclosure.Id)
                    .ToArray();

            int deletedPositions =
                await dbContext.InvestmentPortfolioPositions
                    .Where(position =>
                        position.ParentCompanyId ==
                        company.CompanyId)
                    .ExecuteDeleteAsync(cancellationToken);

            int deletedMetadata =
                disclosureIds.Length == 0
                    ? 0
                    : await dbContext
                        .InvestmentPortfolioReportMetadata
                        .Where(metadata =>
                            disclosureIds.Contains(
                                metadata.DisclosureId))
                        .ExecuteDeleteAsync(cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Portfolio company rebuild starting. CompanyId={CompanyId}, Symbol={Symbol}, Disclosures={DisclosureCount}, DeletedPositions={DeletedPositions}, DeletedMetadata={DeletedMetadata}",
                    company.CompanyId,
                    company.SymbolKey,
                    companyDisclosures.Count,
                    deletedPositions,
                    deletedMetadata);
            }

            foreach (Disclosure disclosure in companyDisclosures)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await portfolioProcessor.ProcessBackfillAsync(
                    disclosure,
                    cancellationToken);

                processedDisclosureCount++;
            }

            processedCompanyCount++;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Portfolio company rebuild finished. CompanyId={CompanyId}, Symbol={Symbol}, Company={CompanyNumber}/{CompanyTotal}",
                    company.CompanyId,
                    company.SymbolKey,
                    processedCompanyCount,
                    companies.Length);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Portfolio rebuild finished. Companies={CompanyCount}, Disclosures={DisclosureCount}",
                processedCompanyCount,
                processedDisclosureCount);
        }
    }

    public async Task<PortfolioHistoryRebuildPlan> BuildPlanAsync(
    CancellationToken cancellationToken = default)
    {
        List<string> metadataSymbols =
            await dbContext.InvestmentPortfolioReportMetadata
                .AsNoTracking()
                .Join(
                    dbContext.Disclosures,
                    metadata => metadata.DisclosureId,
                    disclosure => disclosure.Id,
                    (_, disclosure) => disclosure.Symbol)
                .Where(symbol =>
                    !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol!)
                .Distinct()
                .ToListAsync(cancellationToken);

        List<int> parentCompanyIds =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .Select(position => position.ParentCompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

        List<string> positionSymbols =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(company =>
                    parentCompanyIds.Contains(company.CompanyId) &&
                    !string.IsNullOrWhiteSpace(company.Symbol))
                .Select(company => company.Symbol!)
                .Distinct()
                .ToListAsync(cancellationToken);

        HashSet<string> metadataSymbolKeys =
            metadataSymbols
                .Select(NormalizeForMatch)
                .ToHashSet(StringComparer.Ordinal);

        HashSet<string> positionSymbolKeys =
            positionSymbols
                .Select(NormalizeForMatch)
                .ToHashSet(StringComparer.Ordinal);

        HashSet<string> portfolioSymbolKeys =
            metadataSymbolKeys
                .Concat(positionSymbolKeys)
                .ToHashSet(StringComparer.Ordinal);

        int positionOnlySymbolCount =
            positionSymbolKeys
                .Except(metadataSymbolKeys)
                .Count();

        var companyRows =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(company =>
                    company.IsListed &&
                    !string.IsNullOrWhiteSpace(company.FSortSymbol))
                .Select(company => new
                {
                    company.CompanyId,
                    company.FSortSymbol
                })
                .ToListAsync(cancellationToken);

        Dictionary<string, int[]> companyIdsBySymbol =
            companyRows
                .GroupBy(company => company.FSortSymbol!)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(company => company.CompanyId)
                        .Distinct()
                        .ToArray(),
                    StringComparer.Ordinal);

        int unmatchedCompanyCount =
            portfolioSymbolKeys.Count(symbol =>
                !companyIdsBySymbol.ContainsKey(symbol));

        int ambiguousCompanyCount =
            portfolioSymbolKeys.Count(symbol =>
                companyIdsBySymbol.TryGetValue(
                    symbol,
                    out int[]? companyIds) &&
                companyIds.Length > 1);

        int[] portfolioCompanyIds =
            portfolioSymbolKeys
                .Where(symbol =>
                    companyIdsBySymbol.TryGetValue(
                        symbol,
                        out int[]? companyIds) &&
                    companyIds.Length == 1)
                .Select(symbol =>
                    companyIdsBySymbol[symbol][0])
                .Distinct()
                .ToArray();

        var candidateDisclosures =
            await dbContext.Disclosures
                .AsNoTracking()
                .Where(disclosure =>
                    disclosure.Rt == 2 &&
                    (disclosure.Let == 58 ||
                     disclosure.Let == 6) &&
                    !string.IsNullOrWhiteSpace(disclosure.Symbol))
                .Select(disclosure => new
                {
                    disclosure.Symbol
                })
                .ToListAsync(cancellationToken);

        int disclosureCount =
            candidateDisclosures.Count(disclosure =>
                portfolioSymbolKeys.Contains(
                    NormalizeForMatch(disclosure.Symbol!)));

        List<string> metadataRowSymbols =
    await dbContext.InvestmentPortfolioReportMetadata
        .AsNoTracking()
        .Join(
            dbContext.Disclosures,
            metadata => metadata.DisclosureId,
            disclosure => disclosure.Id,
            (_, disclosure) => disclosure.Symbol)
        .ToListAsync(cancellationToken);

        int totalMetadataCount =
            metadataRowSymbols.Count;

        int metadataCount =
            metadataRowSymbols.Count(symbol =>
                !string.IsNullOrWhiteSpace(symbol) &&
                portfolioSymbolKeys.Contains(
                    NormalizeForMatch(symbol)));

        int totalPositionCount =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .CountAsync(cancellationToken);

        int positionCount =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .CountAsync(
                    position =>
                        portfolioCompanyIds.Contains(
                            position.ParentCompanyId),
                    cancellationToken);

        return new PortfolioHistoryRebuildPlan(
            PortfolioSymbolCount: portfolioSymbolKeys.Count,
            PositionOnlySymbolCount: positionOnlySymbolCount,
            UnmatchedCompanyCount: unmatchedCompanyCount,
            AmbiguousCompanyCount: ambiguousCompanyCount,
            DisclosureCount: disclosureCount,
            MetadataCount: metadataCount,
            TotalMetadataCount: totalMetadataCount,
            TotalPositionCount: totalPositionCount,
            PositionCount: positionCount);
    }
}
public sealed record PortfolioHistoryRebuildCompany(
    int CompanyId,
    string SymbolKey);
public sealed record PortfolioHistoryRebuildPlan(
    int PortfolioSymbolCount,
    int PositionOnlySymbolCount,
    int UnmatchedCompanyCount,
    int AmbiguousCompanyCount,
    int DisclosureCount,
    int MetadataCount,
    int TotalMetadataCount,
    int TotalPositionCount,
    int PositionCount);
public sealed record PortfolioHistoryRebuildResult(
    int ProcessedDisclosureCount,
    int MetadataCount,
    int PositionCount);
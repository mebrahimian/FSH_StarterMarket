using FSH.Framework.Shared.Utilities;
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Companies;

public sealed class CompanyIdBackfillService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<CompanyIdBackfillResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var companies =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(x =>
                    x.IsListed &&
                    x.Symbol != null)
                .Select(x => new
                {
                    x.CompanyId,
                    x.Symbol
                })
                .ToListAsync(cancellationToken);

        var companyLookup =
            companies
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Symbol))
                .GroupBy(
                    x => FSort.Normalize(x.Symbol!),
                    StringComparer.Ordinal)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .Select(y => y.CompanyId)
                        .Distinct()
                        .ToArray(),
                    StringComparer.Ordinal);

        List<string> disclosureSymbols =
            await dbContext.Disclosures
                .AsNoTracking()
                .Where(x =>
                    x.CompanyId == null &&
                    x.Symbol != "")
                .Select(x => x.Symbol)
                .Distinct()
                .ToListAsync(cancellationToken);

        int updatedRows = 0;
        int matchedSymbols = 0;
        int unmatchedSymbols = 0;
        int ambiguousSymbols = 0;

        foreach (string symbol in disclosureSymbols)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            string normalizedSymbol =
                FSort.Normalize(symbol);

            if (!companyLookup.TryGetValue(
                    normalizedSymbol,
                    out int[]? companyIds))
            {
                unmatchedSymbols++;
                continue;
            }

            if (companyIds.Length != 1)
            {
                ambiguousSymbols++;
                continue;
            }

            int companyId =
                companyIds[0];

            int updated =
                await dbContext.Disclosures
                    .Where(x =>
                        x.CompanyId == null &&
                        x.Symbol == symbol)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                x => x.CompanyId,
                                companyId),
                        cancellationToken);

            updatedRows += updated;
            matchedSymbols++;
            
            await ReconcilePortfolioAsync(
                symbol,
                companyId,
                cancellationToken);
            await ReconcileMonthlyActivitySummariesAsync(
                companyId,
                cancellationToken);

        }

        return new CompanyIdBackfillResult(
            updatedRows,
            matchedSymbols,
            unmatchedSymbols,
            ambiguousSymbols);
    }
    private async Task ReconcilePortfolioAsync(
    string symbol,
    int companyId,
    CancellationToken cancellationToken)
    {
        string? companyName =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .Select(x => x.CompanyName)
                .SingleOrDefaultAsync(cancellationToken);

        string fSortSymbol = FSort.Normalize(symbol);

        string? fSortName = string.IsNullOrWhiteSpace(companyName)
                ? null
                : FSort.Normalize(companyName);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
        DECLARE @MatchedAliases TABLE
        (
            OldCompanyId int,
            FSortName nvarchar(512),
            IsListed bit
        );

        INSERT INTO @MatchedAliases
        (
            OldCompanyId,
            FSortName,
            IsListed
        )
        SELECT DISTINCT
            CompanyId,
            FSortName,
            IsListed
        FROM marketintelligence.PortfolioCompanyAliases
        WHERE IsActive = 1
          AND
          (
              FSortSymbol = {fSortSymbol}
              OR ({fSortName} IS NOT NULL AND FSortName = {fSortName})
          );

        UPDATE p
        SET ChildCompanyId = {companyId}
        FROM marketintelligence.InvestmentPortfolioPositions p
        INNER JOIN @MatchedAliases a
            ON p.ChildCompanyId = a.OldCompanyId
        WHERE p.ChildCompanyId <> {companyId};

        UPDATE p
        SET ChildCompanyId = {companyId}
        FROM marketintelligence.InvestmentPortfolioPositions p
        INNER JOIN @MatchedAliases a
            ON p.FSortName = a.FSortName
           AND p.IsListed = a.IsListed
        WHERE p.ChildCompanyId IS NULL;

        UPDATE a
        SET CompanyId = {companyId}
        FROM marketintelligence.PortfolioCompanyAliases a
        WHERE a.IsActive = 1
          AND
          (
              a.FSortSymbol = {fSortSymbol}
              OR ({fSortName} IS NOT NULL AND FSortName = {fSortName})
          );
        """,
            cancellationToken);
    }
    private async Task ReconcileMonthlyActivitySummariesAsync(
    int companyId,
    CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
        UPDATE s
        SET s.CompanyId = {companyId}
        FROM marketintelligence.MonthlyActivitySummaries s
        INNER JOIN marketintelligence.Disclosures d
            ON d.Id = s.DisclosureId
        WHERE s.CompanyId IS NULL
          AND d.CompanyId = {companyId};
        """,
            cancellationToken);
    }
}

public sealed record CompanyIdBackfillResult(
    int UpdatedRows,
    int MatchedSymbols,
    int UnmatchedSymbols,
    int AmbiguousSymbols);
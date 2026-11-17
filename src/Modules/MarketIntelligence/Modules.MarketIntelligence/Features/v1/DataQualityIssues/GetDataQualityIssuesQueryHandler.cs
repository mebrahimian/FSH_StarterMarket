using FSH.Framework.Shared.Utilities;
using static FSH.Framework.BuildingBlocks.Shared.Globalization.PersianTextNormalizer;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Contracts.v1.DataQualityIssues;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Modules.MarketIntelligence.Features.v1.DataQualityIssues;

public sealed class GetDataQualityIssuesQueryHandler(
    MarketIntelligenceDbContext dbContext)
    : IQueryHandler<
        GetDataQualityIssuesQuery,
        IReadOnlyList<DataQualityIssueDto>>
{
    public async ValueTask<IReadOnlyList<DataQualityIssueDto>> Handle(
        GetDataQualityIssuesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        DateOnly minTradeDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365));
        var recentlyPricedSymbols = (await dbContext.TsetmcInstruments
            .AsNoTracking()
            .Where(instrument =>
                dbContext.DailyPrices.Any(price =>
                    price.InstrumentId == instrument.Id &&
                    price.TradeDate >= minTradeDate &&
                    (price.ClosingPrice > 0 || price.LastPrice > 0)))
            .Select(instrument => instrument.NormalizedSymbol)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .Select(symbol => NormalizeForMatch(symbol))
            .ToHashSet(StringComparer.Ordinal);

        var summaries = await (
            from summary in dbContext.MonthlyActivitySummaries
                .AsNoTracking()

            join disclosure in dbContext.Disclosures
                .AsNoTracking()
                on summary.DisclosureId equals disclosure.Id
                into disclosureGroup

            from disclosure in disclosureGroup.DefaultIfEmpty()

            select new
            {
                summary.Symbol,
                summary.YearEndDate,
                summary.PeriodEndDate,
                summary.YearToDateAmount,
                summary.PreviousYearToDateAmount,

                PublishDateTimeRaw =
                    disclosure != null
                        ? disclosure.PublishDateTimeRaw
                        : null,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var issues = new List<DataQualityIssueDto>();

        var summariesBySymbolAndPeriod =
            summaries
                .Where(summary =>
                    !string.IsNullOrWhiteSpace(summary.Symbol) &&
                    summary.PeriodEndDate.Length >= 7)
                .GroupBy(summary => new
                {
                    summary.Symbol,
                    Period = summary.PeriodEndDate[..7],
                })
                .ToDictionary(
                    group => (
                        group.Key.Symbol,
                        group.Key.Period),
                    group => group.First());

        foreach (var summary in summaries)
        {
            if (summary.PeriodEndDate.Length < 7 ||
                !summary.PreviousYearToDateAmount.HasValue)
            {
                continue;
            }

            if (!int.TryParse(
                    summary.PeriodEndDate[..4],
                    out int currentYear))
            {
                continue;
            }

            string previousYearPeriod = (currentYear - 1).ToString(
        "0000", System.Globalization.CultureInfo.InvariantCulture)
                     + summary.PeriodEndDate[4..7];

            if (!summariesBySymbolAndPeriod.TryGetValue(
                    (summary.Symbol, previousYearPeriod),
                    out var previousYearSummary))
            {
                continue;
            }

            if (!previousYearSummary.YearToDateAmount.HasValue)
            {
                continue;
            }

            decimal storedValue =
                summary.PreviousYearToDateAmount.Value;

            decimal calculatedValue =
                previousYearSummary.YearToDateAmount.Value;

            if (Math.Abs(storedValue - calculatedValue) <= 1)
            {
                continue;
            }

            string? publishDate =
                PersianDateTextParser.TryExtract(
                    summary.PublishDateTimeRaw);

            issues.Add(
                new DataQualityIssueDto(
                    summary.Symbol,
                    summary.YearEndDate,
                    summary.PeriodEndDate,
                    publishDate,
                    "PreviousYearYtdMismatch",
                    calculatedValue,
                    storedValue));
        }
        var listedCompanies = await dbContext.CompanyMaster
    .AsNoTracking()
    .Where(x => x.IsListed && x.FSortSymbol != null)
    .Select(x => new
    {
        x.CompanyId,
        x.FSortSymbol
    })
    .ToListAsync(cancellationToken)
    .ConfigureAwait(false);

        var eligibleCompanies = listedCompanies
            .Where(x => recentlyPricedSymbols.Contains(
                NormalizeForMatch(x.FSortSymbol!)))
            .ToList();

        var listedCompanySymbols = eligibleCompanies
            .Select(x => NormalizeForMatch(x.FSortSymbol!))
            .ToHashSet(StringComparer.Ordinal);

        int[] eligibleCompanyIds = eligibleCompanies
            .Select(x => x.CompanyId)
            .Distinct()
            .ToArray();

        var missingPortfolioDisclosures = (await dbContext.Disclosures
            .AsNoTracking()
            .Where(disclosure =>
                (disclosure.Let == 58 || disclosure.Let == 6) &&
                disclosure.Rt == 2 &&
                disclosure.Ct != 5 &&
                disclosure.HasHtml &&

                !dbContext.InvestmentPortfolioPositions
                    .Any(position =>
                        position.DisclosureId == disclosure.Id) &&

                // Ignore reports replaced by newer portfolio positions.
                !dbContext.InvestmentPortfolioReportMetadata
                    .Any(metadata =>
                        metadata.DisclosureId == disclosure.Id &&
                        metadata.PeriodEndToDate != null &&

                        dbContext.InvestmentPortfolioPositions
                            .Any(position =>
                                position.ParentCompanyId == disclosure.CompanyId &&
                                position.PeriodEndDate == metadata.PeriodEndToDate &&
                                position.SourceType == metadata.SourceType &&
                                position.AuditStatus == metadata.AuditStatus &&
                                disclosure.PublishDateTime != null &&
                                position.PublishDateTime != null &&
                                (
                                    position.PublishDateTime > disclosure.PublishDateTime ||
                                    (
                                        position.PublishDateTime == disclosure.PublishDateTime &&
                                        position.TracingNo > disclosure.TracingNo
                                    )
                                ))))
            .Select(disclosure => new
            {
                disclosure.Symbol,
                disclosure.Title,
                disclosure.Let,
                disclosure.PublishDateTimeRaw,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .Where(disclosure =>
                listedCompanySymbols.Contains(
                    NormalizeForMatch(disclosure.Symbol)) &&
                (disclosure.Let == 58 ||
                 IsMainFinancialReport(disclosure.Title)))
            .ToList();


        foreach (var disclosure in missingPortfolioDisclosures)
        {
            string periodEndDate =
                PersianDateTextParser.TryExtract(
                    disclosure.Title)
                ?? string.Empty;

            string? publishDate =
                PersianDateTextParser.TryExtract(
                    disclosure.PublishDateTimeRaw);

            issues.Add(
                new DataQualityIssueDto(
                    disclosure.Symbol,
                    null,
                    periodEndDate,
                    publishDate,
                    "MissingPortfolio",
                    null,
                    null));
        }
        var incompletePortfolioMetadataDisclosures =
    await dbContext.Disclosures
        .AsNoTracking()
        .Where(disclosure =>
           disclosure.CompanyId.HasValue &&
           eligibleCompanyIds.Contains(disclosure.CompanyId.Value) &&
           dbContext.InvestmentPortfolioPositions
        .Any(position =>
           position.DisclosureId == disclosure.Id) &&
            (
                !dbContext.InvestmentPortfolioReportMetadata
                    .Any(metadata =>
                        metadata.DisclosureId ==
                        disclosure.Id) ||

                !dbContext.InvestmentPortfolioReportMetadata
                    .Any(metadata =>
                        metadata.DisclosureId ==
                            disclosure.Id &&
                        metadata.ReportSymbol != null &&
                        metadata.ReportSymbol.Trim() !=
                            string.Empty) ||

                !dbContext.InvestmentPortfolioReportMetadata
                    .Any(metadata =>
                        metadata.DisclosureId ==
                            disclosure.Id &&
                        metadata.ReportCompanyName != null &&
                        metadata.ReportCompanyName.Trim() !=
                            string.Empty) ||

                !dbContext.InvestmentPortfolioReportMetadata
                    .Any(metadata =>
                        metadata.DisclosureId ==
                            disclosure.Id &&
                        metadata.RegisteredCapital.HasValue)
            ))
        .Select(disclosure => new
        {
            disclosure.Symbol,
            disclosure.Title,
            disclosure.PublishDateTimeRaw,
        })
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

        foreach (var disclosure in incompletePortfolioMetadataDisclosures)
        {
            string periodEndDate =
                PersianDateTextParser.TryExtract(
                    disclosure.Title)
                ?? string.Empty;

            string? publishDate =
                PersianDateTextParser.TryExtract(
                    disclosure.PublishDateTimeRaw);

            issues.Add(
                new DataQualityIssueDto(
                    disclosure.Symbol,
                    null,
                    periodEndDate,
                    publishDate,
                    "IncompletePortfolioMetadata",
                    null,
                    null));
        }
        
        var salesHistoryGapIssues =
    new List<(DataQualityIssueDto Issue, int MissingMonths)>();

        var salesSnapshots = await dbContext.SalesPerformanceSnapshots
            .AsNoTracking()
            .Where(snapshot =>
               eligibleCompanyIds.Contains(snapshot.CompanyId) &&
               snapshot.Symbol != null &&
               snapshot.PeriodEndDate != null &&
               snapshot.PeriodEndDate.Length >= 7)
            .Select(snapshot => new
            {
                snapshot.CompanyId,
                snapshot.Symbol,
                snapshot.PeriodEndDate,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var salesHistoryGroups = salesSnapshots
            .GroupBy(snapshot => new
            {
                snapshot.CompanyId,
                Symbol = snapshot.Symbol.Trim(),
            });

        foreach (var group in salesHistoryGroups)
        {
            var existingMonths = group
                .Select(snapshot =>
                {
                    if (!int.TryParse(
                            snapshot.PeriodEndDate.AsSpan(0, 4),
                            out int year) ||
                        !int.TryParse(
                            snapshot.PeriodEndDate.AsSpan(5, 2),
                            out int month))
                    {
                        return (int?)null;
                    }

                    return year * 12 + month;
                })
                .Where(monthNo => monthNo.HasValue)
                .Select(monthNo => monthNo!.Value)
                .Distinct()
                .OrderBy(monthNo => monthNo)
                .ToList();

            if (existingMonths.Count < 2)
            {
                continue;
            }

            int firstMonthNo = existingMonths[0];
            int lastMonthNo = existingMonths[^1];

            var existingMonthSet = existingMonths.ToHashSet();

            var missingMonths = new List<int>();

            for (int monthNo = firstMonthNo;
                 monthNo <= lastMonthNo;
                 monthNo++)
            {
                if (!existingMonthSet.Contains(monthNo))
                {
                    missingMonths.Add(monthNo);
                }
            }

            if (missingMonths.Count == 0)
            {
                continue;
            }

            int firstMissingMonthNo = missingMonths[0];
            int lastMissingMonthNo = missingMonths[^1];

            string firstMissingPeriod =
                ToPersianYearMonth(firstMissingMonthNo);

            string lastMissingPeriod =
                ToPersianYearMonth(lastMissingMonthNo);

            var issue = new DataQualityIssueDto(
                group.Key.Symbol,
                null,
                firstMissingPeriod,
                lastMissingPeriod,
                "SalesHistoryGap",
                missingMonths.Count,
                null);

            salesHistoryGapIssues.Add(
                (issue, missingMonths.Count));
        }

        issues.AddRange(
            salesHistoryGapIssues
                .OrderByDescending(x => x.MissingMonths)
                .ThenBy(x => x.Issue.Symbol)
                .Select(x => x.Issue));

        return issues
            .Where(issue => listedCompanySymbols.Contains(NormalizeForMatch(issue.Symbol)))
            .ToList();
    }
    
    private static string ToPersianYearMonth(
    int monthNo)
    {
        int year = (monthNo - 1) / 12;
        int month = ((monthNo - 1) % 12) + 1;

        return year.ToString(
                   "0000",
                   System.Globalization.CultureInfo.InvariantCulture)
               + "/"
               + month.ToString(
                   "00",
                   System.Globalization.CultureInfo.InvariantCulture);
    }
    private static bool IsMainFinancialReport(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        string cleanTitle = title.Replace(
            "(اصلاحیه)", "", StringComparison.Ordinal);

        int opens = cleanTitle.Count(c => c == '(');
        int closes = cleanTitle.Count(c => c == ')');

        return opens == 1 && closes == 1;
    }
}
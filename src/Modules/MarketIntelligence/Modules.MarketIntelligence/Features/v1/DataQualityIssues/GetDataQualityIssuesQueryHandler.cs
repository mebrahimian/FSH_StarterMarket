using FSH.Framework.Shared.Utilities;
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
        var listedCompanySymbols = (await dbContext.CompanyMaster
                     .AsNoTracking()
                     .Where(x => x.IsListed &&
                                 x.FSortSymbol != null)
                     .Select(x => x.FSortSymbol!)
                     .ToListAsync(cancellationToken)
                     .ConfigureAwait(false))
                     .ToHashSet(StringComparer.Ordinal);
        var missingPortfolioDisclosures = (await dbContext.Disclosures
                     .AsNoTracking()
                     .Where(disclosure => disclosure.Let == 58 &&
                                          disclosure.Rt == 2 &&
                                          disclosure.HasHtml &&
                                          !dbContext.InvestmentPortfolioPositions
                    .Any(position => position.DisclosureId == disclosure.Id))
                    .Select(disclosure => new
                                              {
                                                 disclosure.Symbol,
                                                 disclosure.Title,
                                                 disclosure.PublishDateTimeRaw,
                                              })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                    .Where(disclosure => listedCompanySymbols.Contains(FSort.Normalize(disclosure.Symbol)))
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
        return issues;
    }
}
using FSH.Framework.Shared.Dates;
using FSH.Framework.Shared.Utilities;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Contracts.v1.FiscalYearSales;
using FSH.Modules.MarketIntelligence.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Features.v1.FiscalYearSales;

public sealed class GetFiscalYearSalesQueryHandler(
    MarketIntelligenceDbContext dbContext)
    : IQueryHandler<GetFiscalYearSalesQuery, FiscalYearSalesDto>
{
    public async ValueTask<FiscalYearSalesDto> Handle(
        GetFiscalYearSalesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string? reportDate = null;

        if (string.IsNullOrWhiteSpace(query.YearEndDate))
        {
            reportDate =
                PersianDateTextParser.TryExtract(query.Title);

            if (reportDate is null)
            {
                return new FiscalYearSalesDto(
                    query.Symbol,
                    string.Empty,
                    []);
            }
        }

        var yearEndDates = await dbContext.MonthlyActivitySummaries
            .AsNoTracking()
            .Where(summary =>
                summary.Symbol == query.Symbol &&
                summary.YearEndDate != null)
            .Select(summary => summary.YearEndDate!)
            .Distinct()
            .OrderBy(yearEndDate => yearEndDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        string? yearEndDate;

        if (!string.IsNullOrWhiteSpace(query.YearEndDate))
        {
            yearEndDate = yearEndDates
                .FirstOrDefault(date =>
                    string.Equals(
                        date,
                        query.YearEndDate,
                        StringComparison.Ordinal));
        }
        else
        {
            yearEndDate = yearEndDates
                .FirstOrDefault(date =>
                    string.CompareOrdinal(
                        date,
                        reportDate) >= 0);
        }

        if (yearEndDate is null)
        {
            return new FiscalYearSalesDto(
                query.Symbol,
                string.Empty,
                []);
        }

        int yearEndDateIndex =  yearEndDates.IndexOf(yearEndDate);

        string? previousYearEndDate = yearEndDateIndex > 0
                                        ? yearEndDates[yearEndDateIndex - 1]
                                        : null;

        string? nextYearEndDate =  yearEndDateIndex >= 0 &&
                                   yearEndDateIndex < yearEndDates.Count - 1
                                        ? yearEndDates[yearEndDateIndex + 1]
                                        : null;
        var summaries = await dbContext.MonthlyActivitySummaries
            .AsNoTracking()
            .Where(summary =>
                summary.Symbol == query.Symbol &&
                summary.YearEndDate == yearEndDate)
            .OrderBy(summary => summary.PeriodEndDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var expectedPeriodEndDates =
    PersianDateHelper.GetMonthEndDatesEndingAt(yearEndDate, 12);

        var summaryByPeriod = summaries
            .GroupBy(
                summary => summary.PeriodEndDate,
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(summary => summary.ParsedAt)
                    .First(),
                StringComparer.Ordinal);

        var rows = expectedPeriodEndDates
            .Select(periodEndDate =>
            {
                if (summaryByPeriod.TryGetValue(
                    periodEndDate,
                    out var summary))
                {
                    return new FiscalYearSalesRowDto(
                        summary.PeriodEndDate,
                        summary.PeriodAmount,
                        summary.YearToDateAmount,
                        summary.PreviousYearToDateAmount,
                        IsMissing: false);
                }

                return new FiscalYearSalesRowDto(
                    periodEndDate,
                    PeriodAmount: null,
                    YearToDateAmount: null,
                    PreviousYearToDateAmount: null,
                    IsMissing: true);
            })
            .ToList();

        return new FiscalYearSalesDto(query.Symbol,
                                      yearEndDate,
                                      rows)
        {
            PreviousYearEndDate = previousYearEndDate,
            NextYearEndDate = nextYearEndDate,
        };
    }
}
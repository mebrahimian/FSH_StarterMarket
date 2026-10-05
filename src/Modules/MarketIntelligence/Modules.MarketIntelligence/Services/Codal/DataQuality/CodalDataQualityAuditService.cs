using FSH.Framework.Shared.Utilities;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using FSH.Modules.MarketIntelligence.Services.Codal.Lookups;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FSH.Modules.MarketIntelligence.Services.Codal.DataQuality;

public sealed class CodalDataQualityAuditService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<CodalDataQualityReport> RunAsync(
        int coverageYears = 5, CancellationToken cancellationToken = default)
    {
        if (coverageYears is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(coverageYears),
                "Coverage years must be between 1 and 20.");
        }

        
        int requiredMonths = coverageYears * 12;
        
        IQueryable<MonthlyActivitySummary> summaries =
            dbContext.MonthlyActivitySummaries.AsNoTracking();

        var persianCalendar =
        new PersianCalendar();

        DateTime auditDateUtc =
            DateTime.UtcNow;

        int currentPersianYear =
            persianCalendar.GetYear(auditDateUtc);

        int currentPersianMonth =
            persianCalendar.GetMonth(auditDateUtc);

        int windowEndYear =
            currentPersianMonth == 1
                ? currentPersianYear - 1
                : currentPersianYear;

        int windowEndMonth =
            currentPersianMonth == 1
                ? 12
                : currentPersianMonth - 1;

        string windowEndPeriod =
            $"{windowEndYear:0000}/{windowEndMonth:00}";

        string[] requiredPeriods = CreatePeriodWindow(
                                  windowEndPeriod,
                                  requiredMonths);
        
        HashSet<string> requiredPeriodSet = requiredPeriods.ToHashSet(
                                StringComparer.Ordinal);

        var summaryPeriods =
    await summaries
        .Where(summary =>
            summary.PeriodEndDate.Length >= 7 &&
            requiredPeriods.Contains(
                summary.PeriodEndDate.Substring(0, 7)))
        .Select(summary => new
        {
            summary.Symbol,
            summary.PeriodEndDate,
        })
        .ToListAsync(cancellationToken);
        var monthlyDisclosures =
    await dbContext.Disclosures
        .AsNoTracking()
        .Where(disclosure =>
            disclosure.Let == 58 ||
            (disclosure.Let == 8 &&
             disclosure.Rt == 2))
        .Select(disclosure => new
        {
            disclosure.Symbol,
            disclosure.Title,
            disclosure.PublishDateTimeRaw,
        })
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

        
        var disclosurePeriodDetails =
    monthlyDisclosures
        .Where(disclosure =>
            !string.IsNullOrWhiteSpace(
                disclosure.Symbol))
        .Select(disclosure => new
        {
            disclosure.Symbol,
            PeriodEndDate =
                PersianDateTextParser.TryExtract(
                    disclosure.Title),
            PublishDate =
                PersianDateTextParser.TryExtract(
                    disclosure.PublishDateTimeRaw),
        })
        .Where(item =>
            item.PeriodEndDate is not null &&
            item.PeriodEndDate.Length >= 7)
        .Select(item => new
        {
            Symbol = item.Symbol!,
            PeriodEndDate = item.PeriodEndDate!,
            Period = item.PeriodEndDate![..7],
            item.PublishDate,
        })
        .Where(item =>
            requiredPeriodSet.Contains(
                item.Period))
        .GroupBy(item => (
            item.Symbol,
            item.Period))
        .ToDictionary(
            group => group.Key,
            group =>
            {
                var item = group
                    .OrderByDescending(x =>
                        x.PublishDate)
                    .First();

                return new CodalMissingPeriod(
                    item.PeriodEndDate,
                    item.PublishDate);
            });

        Dictionary<string, string[]> reportedPeriodsBySymbol =
            disclosurePeriodDetails.Keys
              .GroupBy(
                  item => item.Symbol,
                  StringComparer.Ordinal)
              .ToDictionary(
                  group => group.Key,
                  group => group
                     .Select(item => item.Period)
                     .OrderBy(period => period)
                     .ToArray(),
             StringComparer.Ordinal);
        string[] symbolsWithMonthlyDisclosures = reportedPeriodsBySymbol.Keys
           .OrderBy(symbol => symbol)
           .ToArray();
        Dictionary<string, HashSet<string>>
            coveragePeriodsBySymbol =
                summaryPeriods
                    .Where(summary =>
                        !string.IsNullOrWhiteSpace(
                            summary.Symbol) &&
                        summary.PeriodEndDate.Length >= 7)
                    .Select(summary => new
                    {
                        summary.Symbol,
                        Period =
                            summary.PeriodEndDate[..7],
                    })
                    .Where(item =>
                        requiredPeriodSet.Contains(
                            item.Period))
                    .GroupBy(item =>
                        item.Symbol)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(item =>
                                item.Period)
                            .ToHashSet(
                                StringComparer.Ordinal),
                        StringComparer.Ordinal);
        List<CodalSymbolCoverageGap> coverageGaps = [];

        foreach (string symbol in symbolsWithMonthlyDisclosures)
        {
            if (
                !coveragePeriodsBySymbol.TryGetValue(
                    symbol,
                    out HashSet<string>? symbolPeriods))
            {
                symbolPeriods = [];
            }

            if (!reportedPeriodsBySymbol.TryGetValue(symbol, out string[]? reportedPeriodsForSymbol))
            {
                reportedPeriodsForSymbol = [];
            }

            string[] missingPeriods =
                reportedPeriodsForSymbol
                    .Where(period =>
                        !symbolPeriods.Contains(period))
                    .ToArray();

            if (missingPeriods.Length == 0)
            {
                continue;
            }

            string[] availablePeriodsForSymbol =
                reportedPeriodsForSymbol
                    .Where(period =>
                        symbolPeriods.Contains(period))
                    .ToArray();

            coverageGaps.Add(
                new CodalSymbolCoverageGap
                {
                    Symbol = symbol,

                    AvailableMonths =
                        availablePeriodsForSymbol.Length,

                    MissingMonths =
                        missingPeriods.Length,

                    OldestAvailablePeriod =
                        availablePeriodsForSymbol
                            .FirstOrDefault(),

                    NewestAvailablePeriod =
                        availablePeriodsForSymbol
                            .LastOrDefault(),

                    MissingPeriods =
                        missingPeriods,

                    MissingPeriodDetails =
                        missingPeriods
                            .Select(period =>
                                disclosurePeriodDetails[
                                    (symbol, period)])
                            .ToArray(),
                });
        }
       
        
        return new CodalDataQualityReport
        {
            
            HistoryCoverage = new CodalHistoryCoverageQuality
            {                
                Gaps = coverageGaps.OrderByDescending(gap =>
                       gap.MissingMonths).ThenBy(gap =>
                            gap.Symbol).ToArray(),
            },
        };
    }
    private static string[] CreatePeriodWindow(
            string? endPeriod,
            int requiredMonths)
    {
        if (
            string.IsNullOrWhiteSpace(endPeriod) ||
            endPeriod.Length < 7 ||
            endPeriod[4] != '/' ||
            !int.TryParse(
                endPeriod[..4],
                out int year) ||
            !int.TryParse(endPeriod.AsSpan(5, 2),
                          out int month) ||
            month is < 1 or > 12)
        {
            return [];
        }

        int endMonthIndex =
            (year * 12) + month - 1;

        return Enumerable
            .Range(0, requiredMonths)
            .Select(index =>
            {
                int monthIndex =
                    endMonthIndex -
                    requiredMonths +
                    index +
                    1;

                int periodYear =
                    monthIndex / 12;

                int periodMonth =
                    (monthIndex % 12) + 1;

                return
                    $"{periodYear:0000}/{periodMonth:00}";
            })
            .ToArray();
    }
    
    

}
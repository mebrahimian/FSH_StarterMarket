using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
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

        DateTime checkedAtUtc = DateTime.UtcNow;
        int requiredMonths = coverageYears * 12;

        CodalDefinitions definitions = CodalDefinitionsProvider.Load();

        byte[] supportedReportTypes = definitions.MonthlyActivities.Keys.ToArray();

        IQueryable<Disclosure> disclosures =
            dbContext.Disclosures.AsNoTracking();

        IQueryable<MonthlyActivitySummary> summaries =
            dbContext.MonthlyActivitySummaries.AsNoTracking();

        var monthlyCandidates =
            disclosures.Where(d =>
                d.Let == 58 &&
                d.Rt.HasValue &&
                supportedReportTypes.Contains(
                    d.Rt.Value));

        var metadata = new CodalMetadataQuality
        {
            TotalDisclosures =
                await disclosures.CountAsync(
                    cancellationToken),

            MissingSymbol =
                await disclosures.CountAsync(
                    d => d.Symbol == string.Empty,
                    cancellationToken),

            MissingUrl =
                await disclosures.CountAsync(
                    d => d.Url == string.Empty,
                    cancellationToken),

            MissingPublishDate =
                await disclosures.CountAsync(
                    d => d.PublishDateTime == null,
                    cancellationToken),

            MissingLet =
                await disclosures.CountAsync(
                    d => d.Let == null,
                    cancellationToken),

            MissingRt =
                await disclosures.CountAsync(
                    d => d.Rt == null,
                    cancellationToken),
        };

        var monthlyProcessing =
            new CodalMonthlyProcessingQuality
            {
                TotalCandidates =
                    await monthlyCandidates.CountAsync(
                        cancellationToken),

                Pending =
                    await monthlyCandidates.CountAsync(
                        d => d.SalesParseStatus ==
                            DisclosureParseStatus.Pending,
                        cancellationToken),

                Success =
                    await monthlyCandidates.CountAsync(
                        d => d.SalesParseStatus ==
                            DisclosureParseStatus.Success,
                        cancellationToken),

                Failed =
                    await monthlyCandidates.CountAsync(
                        d => d.SalesParseStatus ==
                            DisclosureParseStatus.Failed,
                        cancellationToken),

                NoData =
                    await monthlyCandidates.CountAsync(
                        d => d.SalesParseStatus ==
                            DisclosureParseStatus.NoData,
                        cancellationToken),

                Skipped =
                    await monthlyCandidates.CountAsync(
                        d => d.SalesParseStatus ==
                            DisclosureParseStatus.Skipped,
                        cancellationToken),
            };

        int missingSourceDisclosure =
            await summaries.CountAsync(
                summary =>
                    summary.DisclosureId == null ||
                    !disclosures.Any(disclosure =>
                        disclosure.Id ==
                        summary.DisclosureId.Value),
                cancellationToken);

        int sourceIdentityMismatch =
            await summaries.CountAsync(
                summary =>
                    summary.DisclosureId != null &&
                    disclosures.Any(disclosure =>
                        disclosure.Id ==
                            summary.DisclosureId.Value &&
                        (
                            summary.TracingNo == null ||
                            disclosure.TracingNo !=
                                summary.TracingNo.Value
                        )),
                cancellationToken);

        int sourceSymbolMismatch =
            await summaries.CountAsync(
                summary =>
                    summary.DisclosureId != null &&
                    disclosures.Any(disclosure =>
                        disclosure.Id ==
                            summary.DisclosureId.Value &&
                        disclosure.Symbol !=
                            summary.Symbol),
                cancellationToken);

        int sourcePublishDateMismatch =
            await summaries.CountAsync(
                summary =>
                    summary.DisclosureId != null &&
                    disclosures.Any(disclosure =>
                        disclosure.Id ==
                            summary.DisclosureId.Value &&
                        disclosure.PublishDateTime !=
                            summary.PublishDateTime),
                cancellationToken);

        int sourceStatusNotSuccess =
            await summaries.CountAsync(
                summary =>
                    summary.DisclosureId != null &&
                    disclosures.Any(disclosure =>
                        disclosure.Id ==
                            summary.DisclosureId.Value &&
                        disclosure.SalesParseStatus !=
                            DisclosureParseStatus.Success),
                cancellationToken);

        int duplicateSymbolPeriods =
            await summaries
                .GroupBy(summary => new
                {
                    summary.Symbol,
                    summary.PeriodEndDate,
                })
                .Where(group => group.Count() > 1)
                .CountAsync(cancellationToken);

        var summaryPeriods =
            await summaries
                .Select(summary => new
                {
                    summary.Symbol,
                    summary.PeriodEndDate,
                    summary.PreviousYearToDateAmount,
                })
                .ToListAsync(cancellationToken);
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
        DateTime activeSinceUtc = auditDateUtc.AddMonths(-12);
        string[] activeSymbols =
            await monthlyCandidates
                .Where(disclosure =>
                    disclosure.PublishDateTime.HasValue &&
                    disclosure.PublishDateTime.Value >=
                        activeSinceUtc)
                .Select(disclosure =>
                    disclosure.Symbol)
                .Distinct()
                .OrderBy(symbol => symbol)
                .ToArrayAsync(cancellationToken);

        HashSet<(string Symbol, string PeriodPrefix)>
            availablePeriods =
                summaryPeriods
                    .Where(summary =>
                        summary.PeriodEndDate.Length >= 7)
                    .Select(summary => (
                        summary.Symbol,
                        summary.PeriodEndDate[..7]))
                    .ToHashSet();

        int missingPreviousYearWhenHistoryExists =
            summaryPeriods.Count(summary =>
                summary.PreviousYearToDateAmount is null &&
                PreviousYearSummaryLookup
                    .GetPreviousYearPeriodPrefix(
                        summary.PeriodEndDate)
                    is string previousYearPrefix &&
                availablePeriods.Contains((
                    summary.Symbol,
                    previousYearPrefix)));
        HashSet<string> requiredPeriodSet = requiredPeriods.ToHashSet(
                                StringComparer.Ordinal);

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

        List<CodalSymbolCoverageGap> coverageGaps =
            [];

        foreach (string symbol in activeSymbols)
        {
            if (
                !coveragePeriodsBySymbol.TryGetValue(
                    symbol,
                    out HashSet<string>? symbolPeriods))
            {
                symbolPeriods = [];
            }

            string[] missingPeriods =
                requiredPeriods
                    .Where(period =>
                        !symbolPeriods.Contains(period))
                    .ToArray();

            if (missingPeriods.Length == 0)
            {
                continue;
            }

            string[] availablePeriodsForSymbol =
                symbolPeriods
                    .OrderBy(period => period)
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
                });
        }
        var summaryQuality =
            new CodalSummaryQuality
            {
                TotalSummaries =
                    await summaries.CountAsync(
                        cancellationToken),

                MissingSourceDisclosure =
                    missingSourceDisclosure,

                SourceIdentityMismatch =
                    sourceIdentityMismatch,

                SourceSymbolMismatch =
                    sourceSymbolMismatch,

                SourcePublishDateMismatch =
                    sourcePublishDateMismatch,

                SourceStatusNotSuccess =
                    sourceStatusNotSuccess,

                DuplicateSymbolPeriods =
                    duplicateSymbolPeriods,

                MissingPeriodAmount =
                    await summaries.CountAsync(
                        summary =>
                            summary.PeriodAmount == null,
                        cancellationToken),

                MissingYearToDateAmount =
                    await summaries.CountAsync(
                        summary =>
                            summary.YearToDateAmount == null,
                        cancellationToken),

                MissingPreviousYearWhenHistoryExists =
                    missingPreviousYearWhenHistoryExists,
            };

        return new CodalDataQualityReport
        {
            CheckedAtUtc = auditDateUtc,
            Metadata = metadata,
            MonthlyProcessing = monthlyProcessing,
            HistoryCoverage = new CodalHistoryCoverageQuality
                                     {
                                        CoverageYears = coverageYears,
                                        RequiredMonths =  requiredMonths,
                                        WindowStartPeriod =  requiredPeriods.FirstOrDefault(),
                                        WindowEndPeriod = requiredPeriods.LastOrDefault(),
                                        ActiveSymbols = activeSymbols.Length,
                                        CompleteSymbols =  activeSymbols.Length -  coverageGaps.Count,
                                        IncompleteSymbols = coverageGaps.Count,
                                        Gaps = coverageGaps.OrderByDescending(gap =>
                                               gap.MissingMonths).ThenBy(gap =>
                                                    gap.Symbol).ToArray(),
                                     },
            Summaries = summaryQuality,
        };
    }
    private static string[]
    CreatePeriodWindow(
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
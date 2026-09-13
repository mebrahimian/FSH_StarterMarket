using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain.Insights;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Insights;

public sealed class SalesPerformanceAnalyzer(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<SalesPerformanceMetrics?> AnalyzeAsync(
        int companyId,
        string periodEndDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);

        MonthlyActivitySummary? current =
            await dbContext.MonthlyActivitySummaries
                .AsNoTracking()
                .Where(x =>
                    x.CompanyId == companyId &&
                    x.PeriodEndDate == periodEndDate)
                .OrderByDescending(x => x.PublishDateTime)
                .FirstOrDefaultAsync(cancellationToken);

        if (current?.PeriodAmount is null ||
            current.PeriodEndDate.Length < 7)
        {
            return null;
        }

        List<MonthlyActivitySummary> history =
            await dbContext.MonthlyActivitySummaries
                .AsNoTracking()
                .Where(x =>
                    x.CompanyId == companyId &&
                    x.PeriodAmount.HasValue)
                .ToListAsync(cancellationToken);

        Dictionary<string, decimal> amounts =
            history
                .Where(x => x.PeriodEndDate.Length >= 7)
                .GroupBy(
                    x => x.PeriodEndDate[..7],
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(x => x.PublishDateTime)
                        .First()
                        .PeriodAmount!.Value,
                    StringComparer.Ordinal);

        string currentPeriod =
            current.PeriodEndDate[..7];

        decimal currentAmount =
            current.PeriodAmount.Value;

        decimal? previousYearCurrentAmount =
            TryGetAmount(
                amounts,
                ShiftPeriod(currentPeriod, -12));

        WindowResult? rollingThreeMonth =
            TryGetWindow(
                amounts,
                currentPeriod,
                startOffset: 0,
                monthCount: 3);

        WindowResult? previousYearRollingThreeMonth =
            TryGetWindow(
                amounts,
                currentPeriod,
                startOffset: -12,
                monthCount: 3);

        WindowResult? rollingSixMonth =
            TryGetWindow(
                amounts,
                currentPeriod,
                startOffset: 0,
                monthCount: 6);

        WindowResult? previousYearRollingSixMonth =
            TryGetWindow(
                amounts,
                currentPeriod,
                startOffset: -12,
                monthCount: 6);

        // Historical baselines exclude current month.
        WindowResult? previousThreeMonths =
            TryGetWindow(amounts, currentPeriod, -1, 3);

        WindowResult? previousSixMonths =
            TryGetWindow(amounts, currentPeriod, -1, 6);

        WindowResult? previousTwelveMonths =
            TryGetWindow(amounts, currentPeriod, -1, 12);

        WindowResult? previousTwentyFourMonths =
            TryGetWindow(amounts, currentPeriod, -1, 24);

        WindowResult? previousSixtyMonths =
            TryGetWindow(amounts, currentPeriod, -1, 60);

        return new SalesPerformanceMetrics
        {
            // 1M
            CurrentAmount =
                currentAmount,

            PreviousYearCurrentAmount =
                previousYearCurrentAmount,

            MonthlyYoYGrowthPercent =
                GrowthPercent(
                    currentAmount,
                    previousYearCurrentAmount),

            // 3M rolling
            RollingThreeMonthAmount =
                rollingThreeMonth?.Total,

            PreviousYearRollingThreeMonthAmount =
                previousYearRollingThreeMonth?.Total,

            ThreeMonthYoYGrowthPercent =
                GrowthPercent(
                    rollingThreeMonth?.Total,
                    previousYearRollingThreeMonth?.Total),

            // 6M rolling
            RollingSixMonthAmount =
                rollingSixMonth?.Total,

            PreviousYearRollingSixMonthAmount =
                previousYearRollingSixMonth?.Total,

            SixMonthYoYGrowthPercent =
                GrowthPercent(
                    rollingSixMonth?.Total,
                    previousYearRollingSixMonth?.Total),

            // Fiscal YTD
            YearToDateAmount =
                current.YearToDateAmount,

            PreviousYearToDateAmount =
                current.PreviousYearToDateAmount,

            YearToDateYoYGrowthPercent =
                GrowthPercent(
                    current.YearToDateAmount,
                    current.PreviousYearToDateAmount),

            // Historical averages
            ThreeMonthAverage =
                previousThreeMonths?.Average,

            SixMonthAverage =
                previousSixMonths?.Average,

            TwelveMonthAverage =
                previousTwelveMonths?.Average,

            TwentyFourMonthAverage =
                previousTwentyFourMonths?.Average,

            SixtyMonthAverage =
                previousSixtyMonths?.Average,

            // Current vs averages
            CurrentVsThreeMonthAveragePercent =
                DifferencePercent(
                    currentAmount,
                    previousThreeMonths?.Average),

            CurrentVsSixMonthAveragePercent =
                DifferencePercent(
                    currentAmount,
                    previousSixMonths?.Average),

            CurrentVsTwelveMonthAveragePercent =
                DifferencePercent(
                    currentAmount,
                    previousTwelveMonths?.Average),

            CurrentVsTwentyFourMonthAveragePercent =
                DifferencePercent(
                    currentAmount,
                    previousTwentyFourMonths?.Average),

            CurrentVsSixtyMonthAveragePercent =
                DifferencePercent(
                    currentAmount,
                    previousSixtyMonths?.Average),

            // Historical highs
            ThreeMonthHigh =
                previousThreeMonths?.HighAmount,

            ThreeMonthHighPeriod =
                previousThreeMonths?.HighPeriod,

            SixMonthHigh =
                previousSixMonths?.HighAmount,

            SixMonthHighPeriod =
                previousSixMonths?.HighPeriod,

            TwelveMonthHigh =
                previousTwelveMonths?.HighAmount,

            TwelveMonthHighPeriod =
                previousTwelveMonths?.HighPeriod,

            TwentyFourMonthHigh =
                previousTwentyFourMonths?.HighAmount,

            TwentyFourMonthHighPeriod =
                previousTwentyFourMonths?.HighPeriod,

            SixtyMonthHigh =
                previousSixtyMonths?.HighAmount,

            SixtyMonthHighPeriod =
                previousSixtyMonths?.HighPeriod,

            // Current vs highs
            CurrentVsThreeMonthHighPercent =
                DifferencePercent(
                    currentAmount,
                    previousThreeMonths?.HighAmount),

            CurrentVsSixMonthHighPercent =
                DifferencePercent(
                    currentAmount,
                    previousSixMonths?.HighAmount),

            CurrentVsTwelveMonthHighPercent =
                DifferencePercent(
                    currentAmount,
                    previousTwelveMonths?.HighAmount),

            CurrentVsTwentyFourMonthHighPercent =
                DifferencePercent(
                    currentAmount,
                    previousTwentyFourMonths?.HighAmount),

            CurrentVsSixtyMonthHighPercent =
                DifferencePercent(
                    currentAmount,
                    previousSixtyMonths?.HighAmount),

            AvailableHistoryMonths =
                CountAvailableHistoryMonths(
                    amounts,
                    currentPeriod)
        };
    }

    private static WindowResult? TryGetWindow(
    Dictionary<string, decimal> amounts,
        string currentPeriod,
        int startOffset,
        int monthCount)
    {
        var values =
            new List<PeriodAmount>(monthCount);

        for (int index = 0; index < monthCount; index++)
        {
            string period =
                ShiftPeriod(
                    currentPeriod,
                    startOffset - index);

            if (!amounts.TryGetValue(
                    period,
                    out decimal amount))
            {
                return null;
            }

            values.Add(
                new PeriodAmount(
                    period,
                    amount));
        }

        decimal total =
            values.Sum(x => x.Amount);

        decimal average =
            total / monthCount;

        PeriodAmount high =
            values
                .OrderByDescending(x => x.Amount)
                .ThenByDescending(x => x.Period)
                .First();

        return new WindowResult(
            total,
            average,
            high.Amount,
            high.Period);
    }

    private static decimal? TryGetAmount(Dictionary<string, decimal> amounts, string period)
    {
        return amounts.TryGetValue(
            period,
            out decimal amount)
                ? amount
                : null;
    }

    private static decimal? GrowthPercent(
        decimal? current,
        decimal? previous)
    {
        if (!current.HasValue ||
            !previous.HasValue ||
            previous.Value == 0m)
        {
            return null;
        }

        return Math.Round(
       ((current.Value - previous.Value) /
        Math.Abs(previous.Value)) * 100m,
      2);
    }

    private static decimal? DifferencePercent(
        decimal? current,
        decimal? baseline)
    {
        if (!current.HasValue ||
            !baseline.HasValue ||
            baseline.Value == 0m)
        {
            return null;
        }

        return Math.Round(
            ((current.Value - baseline.Value) /
             baseline.Value) * 100m,
            2);
    }

    private static int CountAvailableHistoryMonths(Dictionary<string, decimal> amounts, string currentPeriod)
    {
        int count = 0;
        for (int offset = 1;
             offset <= 60;
             offset++)
        {
            string period =
                ShiftPeriod(
                    currentPeriod,
                    -offset);

            if (!amounts.ContainsKey(period))
            {
                break;
            }

            count++;
        }

        return count;
    }

    private static string ShiftPeriod(
        string period,
        int monthOffset)
    {
        if (period.Length < 7 ||
            period[4] != '/' ||
            !int.TryParse(period[..4], out int year) ||
            !int.TryParse(period.AsSpan(5, 2), out int month) ||
            month is < 1 or > 12)
        {
            throw new FormatException(
                $"Invalid Persian period '{period}'.");
        }

        int monthIndex =
            (year * 12) +
            month -
            1 +
            monthOffset;

        int shiftedYear =
            monthIndex / 12;

        int shiftedMonth =
            (monthIndex % 12) + 1;

        return $"{shiftedYear:0000}/{shiftedMonth:00}";
    }

    private sealed record PeriodAmount(
        string Period,
        decimal Amount);

    private sealed record WindowResult(
        decimal Total,
        decimal Average,
        decimal HighAmount,
        string HighPeriod);
}
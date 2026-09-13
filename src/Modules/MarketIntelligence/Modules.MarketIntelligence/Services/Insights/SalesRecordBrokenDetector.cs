using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Insights;

public sealed class SalesRecordBrokenDetector(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<SalesRecordBrokenResult?> DetectAsync(
    string symbol,
    string periodEndDate,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);

        string currentPeriod = periodEndDate[..7];

        string[] previousPeriods = GetPreviousPeriods(currentPeriod, 12);

        MonthlyActivitySummary? current =
            await dbContext.MonthlyActivitySummaries
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Symbol == symbol &&
                        x.PeriodEndDate == periodEndDate,
                    cancellationToken);

        if (current?.PeriodAmount is null)
        {
            return null;
        }

        List<MonthlyActivitySummary> previous =
            await dbContext.MonthlyActivitySummaries
                .AsNoTracking()
                .Where(x =>
                    x.Symbol == symbol &&
                    x.PeriodAmount.HasValue &&
                    x.PeriodEndDate.Length >= 7 &&
                    previousPeriods.Contains(
                        x.PeriodEndDate.Substring(0, 7)))
                .ToListAsync(cancellationToken);

        Dictionary<string, MonthlyActivitySummary> previousByPeriod =
            previous
                .GroupBy(x => x.PeriodEndDate[..7])
                .Where(group => group.Count() == 1)
                .ToDictionary(
                    group => group.Key,
                    group => group.Single(),
                    StringComparer.Ordinal);

        // هر 12 ماه تقویمی قبلی باید موجود باشند.
        if (previousPeriods.Any(
            period => !previousByPeriod.ContainsKey(period)))
        {
            return null;
        }

        MonthlyActivitySummary previousRecordRow =
            previousPeriods
                .Select(period => previousByPeriod[period])
                .MaxBy(x => x.PeriodAmount!.Value)!;

        decimal previousRecord =
            previousRecordRow.PeriodAmount!.Value;

        if (current.PeriodAmount.Value <= previousRecord)
        {
            return null;
        }

        decimal changePercent = previousRecord == 0
             ? 0
             : Math.Round(((current.PeriodAmount.Value - previousRecord) / Math.Abs(previousRecord)) * 100, 0);

        return new SalesRecordBrokenResult(
            symbol,
            periodEndDate,
            current.PeriodAmount.Value,
            previousRecord,
            previousRecordRow.PeriodEndDate,
            changePercent);
    }
    private static string[] GetPreviousPeriods(
    string currentPeriod,
    int count)
    {
        if (
            currentPeriod.Length != 7 ||
            currentPeriod[4] != '/' ||
            !int.TryParse(currentPeriod[..4], out int year) ||
            !int.TryParse(currentPeriod.AsSpan(5, 2), out int month) ||
            month is < 1 or > 12)
        {
            return [];
        }

        int currentMonthIndex =
            (year * 12) + month - 1;

        return Enumerable
            .Range(1, count)
            .Select(offset =>
            {
                int monthIndex =
                    currentMonthIndex - offset;

                int periodYear =
                    monthIndex / 12;

                int periodMonth =
                    (monthIndex % 12) + 1;

                return $"{periodYear:0000}/{periodMonth:00}";
            })
            .ToArray();
    }
}

public sealed record SalesRecordBrokenResult(
    string Symbol,
    string PeriodEndDate,
    decimal CurrentAmount,
    decimal PreviousRecordAmount,
    string PreviousRecordPeriod,
    decimal ChangePercent);
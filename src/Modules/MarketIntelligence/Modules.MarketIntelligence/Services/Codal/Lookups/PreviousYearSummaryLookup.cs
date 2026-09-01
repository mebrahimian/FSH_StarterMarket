using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Lookups;

public sealed class PreviousYearSummaryLookup(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<decimal?> FindYearToDateAmountAsync(
        string symbol,
        string? periodEndDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            symbol);

        string? previousYearPeriodPrefix =
            GetPreviousYearPeriodPrefix(
                periodEndDate);

        if (previousYearPeriodPrefix is null)
        {
            return null;
        }

        var summary =
    await dbContext
        .MonthlyActivitySummaries
        .AsNoTracking()
        .Where(x =>
            x.Symbol == symbol &&
            x.PeriodEndDate.StartsWith(
                previousYearPeriodPrefix))
        .OrderByDescending(
            x => x.PublishDateTime)
        .Select(x => new
        {
            x.YearToDateAmount,
        })
        .FirstOrDefaultAsync(
            cancellationToken);

        return summary?.YearToDateAmount;
    }

    internal static string?  GetPreviousYearPeriodPrefix(string? periodEndDate)
    {
        if (
            string.IsNullOrWhiteSpace(
                periodEndDate) ||
            periodEndDate.Length < 7 ||
            !int.TryParse(
                periodEndDate[..4],
                out int year)
        )
        {
            return null;
        }

        return
            $"{year - 1:0000}{periodEndDate[4..7]}";
    }
}
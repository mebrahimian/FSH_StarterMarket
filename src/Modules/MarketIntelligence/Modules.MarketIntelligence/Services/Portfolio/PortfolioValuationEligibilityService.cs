using System.Globalization;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.MarketData;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed class PortfolioValuationEligibilityService(
    MarketIntelligenceDbContext dbContext,
    DailyPriceMarketPriceProvider priceProvider)
{
    public async Task<HashSet<int>> GetEligibleCompanyIdsAsync(
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default)
    {
        var calendar = new PersianCalendar();

        DateTime today =
            evaluationDate.ToDateTime(TimeOnly.MinValue);

        DateTime oneYearAgo =
            calendar.AddYears(today, -1);

        string fromPeriod = FormatPersianDate(oneYearAgo);
        string toPeriod = FormatPersianDate(today);

        // Companies with monthly portfolio reports.
        var latestReports = await dbContext
            .InvestmentPortfolioPositions
            .AsNoTracking()
            .Where(p =>
                p.SourceType == PortfolioSourceType.MonthlyActivity)
            .GroupBy(p => p.ParentCompanyId)
            .Select(g => new
            {
                CompanyId = g.Key,
                LastPeriod = g.Max(p => p.PeriodEndDate)
            })
            .ToListAsync(cancellationToken);

        int[] candidateIds = latestReports
            .Where(x =>
                string.CompareOrdinal(
                    x.LastPeriod, fromPeriod) >= 0 &&
                string.CompareOrdinal(
                    x.LastPeriod, toPeriod) <= 0)
            .Select(x => x.CompanyId)
            .ToArray();

        if (candidateIds.Length == 0)
        {
            return [];
        }

        // Latest available parent-company prices.
        var prices = await priceProvider.GetPricesAsOfAsync(
            candidateIds,
            evaluationDate,
            cancellationToken);

        return prices
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Value.TradeDate) &&
                string.CompareOrdinal(
                    x.Value.TradeDate, fromPeriod) >= 0 &&
                (
                    x.Value.LastPrice is > 0m ||
                    x.Value.ClosingPrice is > 0m
                ))
            .Select(x => x.Key)
            .ToHashSet();
    }

    private static string FormatPersianDate(DateTime date)
    {
        var calendar = new PersianCalendar();

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{calendar.GetYear(date):0000}/" +
            $"{calendar.GetMonth(date):00}/" +
            $"{calendar.GetDayOfMonth(date):00}");
    }
}
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FSH.Modules.MarketIntelligence.Services.MarketData;

public sealed class DailyPriceMarketPriceProvider(
    MarketIntelligenceDbContext dbContext)
    : IMarketPriceProvider
{
    public async Task<IReadOnlyDictionary<int, MarketPriceSnapshot>>
        GetLatestPricesAsync(
            IReadOnlyCollection<int> companyIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(companyIds);

        if (companyIds.Count == 0)
        {
            return new Dictionary<int, MarketPriceSnapshot>();
        }

        int[] requestedCompanyIds =
            companyIds
                .Distinct()
                .ToArray();

        var companies =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .Where(company =>
                    requestedCompanyIds.Contains(company.CompanyId) &&
                    company.FSortSymbol != null)
                .Select(company => new
                {
                    company.CompanyId,
                    company.Symbol,
                    NormalizedSymbol = company.FSortSymbol!
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        if (companies.Count == 0)
        {
            return new Dictionary<int, MarketPriceSnapshot>();
        }

        string[] normalizedSymbols =
            companies
                .Select(company => company.NormalizedSymbol)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        var instruments =
            await dbContext.TsetmcInstruments
                .AsNoTracking()
                .Where(instrument =>
                    normalizedSymbols.Contains(
                        instrument.NormalizedSymbol))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var instrumentBySymbol =
            instruments
                .GroupBy(
                    instrument => instrument.NormalizedSymbol,
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(
                            instrument => instrument.LastSeenAt)
                        .First(),
                    StringComparer.Ordinal);

        var companyInstrumentMap =
            companies
                .Where(company =>
                    instrumentBySymbol.ContainsKey(
                        company.NormalizedSymbol))
                .Select(company => new
                {
                    company.CompanyId,
                    company.Symbol,
                    InstrumentId =
                        instrumentBySymbol[
                            company.NormalizedSymbol].Id
                })
                .ToList();

        if (companyInstrumentMap.Count == 0)
        {
            return new Dictionary<int, MarketPriceSnapshot>();
        }

        int[] instrumentIds =
            companyInstrumentMap
                .Select(x => x.InstrumentId)
                .Distinct()
                .ToArray();

        var latestPrices =
            await dbContext.DailyPrices
                .AsNoTracking()
                .Where(price =>
                    instrumentIds.Contains(price.InstrumentId))
                .GroupBy(price => price.InstrumentId)
                .Select(group =>
                    group
                        .OrderByDescending(price => price.TradeDate)
                        .ThenByDescending(price => price.LastUpdatedAt)
                        .First())
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var priceByInstrumentId =
            latestPrices.ToDictionary(
                price => price.InstrumentId);

        var result =
            new Dictionary<int, MarketPriceSnapshot>();

        foreach (var item in companyInstrumentMap)
        {
            if (!priceByInstrumentId.TryGetValue(
                    item.InstrumentId,
                    out var price))
            {
                continue;
            }

            result[item.CompanyId] =
                new MarketPriceSnapshot(
                    CompanyId: item.CompanyId,
                    Symbol: item.Symbol,
                    LastPrice: price.LastPrice,
                    ClosingPrice: price.ClosingPrice,
                    TradeDate: FormatPersianDate(price.TradeDate));
        }

        return result;
    }
    private static string FormatPersianDate(DateOnly date)
    {
        var persianCalendar = new PersianCalendar();

        DateTime dateTime =
            date.ToDateTime(TimeOnly.MinValue);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{persianCalendar.GetYear(dateTime):0000}/" +
            $"{persianCalendar.GetMonth(dateTime):00}/" +
            $"{persianCalendar.GetDayOfMonth(dateTime):00}");
    }
}
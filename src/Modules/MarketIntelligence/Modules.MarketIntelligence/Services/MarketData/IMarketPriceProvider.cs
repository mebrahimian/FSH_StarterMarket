namespace FSH.Modules.MarketIntelligence.Services.MarketData;

public interface IMarketPriceProvider
{
    Task<IReadOnlyDictionary<int, MarketPriceSnapshot>>
        GetLatestPricesAsync(
            IReadOnlyCollection<int> companyIds,
            CancellationToken cancellationToken = default);
}
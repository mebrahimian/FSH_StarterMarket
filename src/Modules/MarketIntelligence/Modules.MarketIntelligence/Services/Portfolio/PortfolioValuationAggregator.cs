using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Services.MarketData;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed record PortfolioValuationAggregate(
    decimal? RegisteredCapital,
    decimal ListedValue,
    decimal UnlistedValue,
    decimal PortfolioValue,
    decimal? ValuePerShare,
    decimal? SharePrice,
    string? PriceTradeDate,
    decimal? PriceToPortfolioValuePercent,
    int ListedPositionCount,
    int PricedPositionCount);

public static class PortfolioValuationAggregator
{
    public static PortfolioValuationAggregate Calculate(
        int companyId,
        IReadOnlyCollection<InvestmentPortfolioPosition> positions,
        decimal? registeredCapital,
        IReadOnlyDictionary<int, MarketPriceSnapshot> prices)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(prices);

        decimal listedValue = 0m;
        decimal unlistedValue = 0m;

        int listedCount = 0;
        int pricedCount = 0;

        foreach (var position in positions)
        {
            if (position.ParentCompanyId != companyId)
            {
                throw new ArgumentException(
                    "Positions must belong to the requested company.",
                    nameof(positions));
            }

            MarketPriceSnapshot? marketPrice = null;

            if (position.IsListed &&
                position.ChildCompanyId.HasValue)
            {
                prices.TryGetValue(
                    position.ChildCompanyId.Value,
                    out marketPrice);
            }

            var valuation =
                PortfolioPositionValuationCalculator.Calculate(
                    position.IsListed,
                    position.EndingQuantity,
                    position.EndingMarketValue,
                    position.EndingCost,
                    marketPrice);

            if (position.IsListed)
            {
                listedCount++;

                listedValue += valuation.CurrentValue ?? 0m;

                if (valuation.UsesCurrentMarketPrice)
                {
                    pricedCount++;
                }
            }
            else
            {
                unlistedValue += valuation.CurrentValue ?? 0m;
            }
        }

        decimal portfolioValue = listedValue + unlistedValue;

        decimal? valuePerShare =
            registeredCapital is > 0m
                ? Math.Round(
                    portfolioValue / registeredCapital.Value * 1000m,
                    0)
                : null;

        prices.TryGetValue(
            companyId,
            out MarketPriceSnapshot? parentPrice);

        decimal? sharePrice =
            parentPrice?.LastPrice ??
            parentPrice?.ClosingPrice;

        decimal? priceToPortfolioValuePercent =
            valuePerShare is > 0m && sharePrice is > 0m
                ? Math.Round(
                    sharePrice.Value / valuePerShare.Value * 100m,
                    2)
                : null;

        return new PortfolioValuationAggregate(
            registeredCapital,
            listedValue,
            unlistedValue,
            portfolioValue,
            valuePerShare,
            sharePrice,
            parentPrice?.TradeDate,
            priceToPortfolioValuePercent,
            listedCount,
            pricedCount);
    }
}
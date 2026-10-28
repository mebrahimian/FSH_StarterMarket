using FSH.Modules.MarketIntelligence.Services.MarketData;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public readonly record struct PortfolioPositionValuationResult(
    decimal? CurrentPrice,
    decimal? CurrentValue,
    string? CurrentPriceTradeDate,
    bool UsesCurrentMarketPrice);

public static class PortfolioPositionValuationCalculator
{
    // Legacy overload - preserve existing callers
    public static PortfolioPositionValuationResult Calculate(
        bool isLatestPortfolio,
        bool isListed,
        decimal? endingQuantity,
        decimal? endingMarketValue,
        decimal? endingCost,
        MarketPriceSnapshot? marketPrice)
    {
        if (!isLatestPortfolio)
        {
            return default;
        }

        return Calculate(
            isListed,
            endingQuantity,
            endingMarketValue,
            endingCost,
            marketPrice);
    }

    // Shared calculation for historical and current valuation
    public static PortfolioPositionValuationResult Calculate(
        bool isListed,
        decimal? endingQuantity,
        decimal? endingMarketValue,
        decimal? endingCost,
        MarketPriceSnapshot? marketPrice)
    {
        if (!isListed)
        {
            return new(
                null,
                endingCost,
                null,
                false);
        }

        decimal? price =
            marketPrice?.LastPrice ??
            marketPrice?.ClosingPrice;

        bool hasMarketPrice =
            price.HasValue &&
            endingQuantity.HasValue;

        decimal? value =
            hasMarketPrice
                ? endingQuantity!.Value *
                  price!.Value / 1_000_000m
                : endingMarketValue;

        return new(
            price,
            value,
            marketPrice?.TradeDate,
            hasMarketPrice);
    }
}
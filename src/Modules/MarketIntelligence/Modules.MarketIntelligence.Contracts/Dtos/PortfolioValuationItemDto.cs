namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed record PortfolioValuationItemDto(
    int CompanyId,
    string? Symbol,
    string? CompanyName,
    Guid DisclosureId,
    string PeriodEndDate,
    decimal? RegisteredCapital,
    decimal? CurrentListedValue,
    decimal? UnlistedReportedValue,
    decimal? CurrentPortfolioValue,
    decimal? CurrentPortfolioValuePerShare,
    decimal? CurrentSharePrice,
    string? CurrentSharePriceTradeDate,
    decimal? DiscountToPortfolioValuePercent,
    int ListedPositionCount,
    int PricedListedPositionCount,
    bool IsHistorical);
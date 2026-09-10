namespace FSH.Modules.MarketIntelligence.Services.MarketData;

public sealed record MarketPriceSnapshot(
    int CompanyId,
    string? Symbol,
    decimal? LastPrice,
    decimal? ClosingPrice,
    string? TradeDate,
    long? ShareCount);
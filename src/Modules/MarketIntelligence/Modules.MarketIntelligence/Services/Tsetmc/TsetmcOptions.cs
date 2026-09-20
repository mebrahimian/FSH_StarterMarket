namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed class TsetmcOptions
{
    public const string SectionName = "Tsetmc";

    public string MarketWatchUrl { get; set; } = default!;
    public string BaseUrl { get; set; } = string.Empty;

    public string ClosingPriceHistoryPath { get; set; } = string.Empty;

    public string ClosingPriceDailyAllPath { get; set; } = string.Empty;
}
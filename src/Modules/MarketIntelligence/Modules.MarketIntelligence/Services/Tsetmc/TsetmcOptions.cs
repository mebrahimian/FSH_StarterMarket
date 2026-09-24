namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed class TsetmcOptions
{
    public const string SectionName = "Tsetmc";

    public string MarketWatchUrl { get; set; } = default!;
    public string BaseUrl { get; set; } = string.Empty;

    public string ClosingPriceHistoryPath { get; set; } = string.Empty;
    public string InstrumentSearchPath { get; set; } = string.Empty;

    public string InstrumentIdentityPath { get; set; } = string.Empty;
    public string ShareChangePath { get; set; } = default!;
    public string MarketWatchPath { get; set; } = default!;
    public string MarketOverviewPath { get; set; } = string.Empty;
    public string InstrumentInfoPath { get; set; } = default!;
    public string ClientTypeHistoryPath { get; set; } = default!;
    public string ClientTypeAllPath { get; set; } = string.Empty;
}
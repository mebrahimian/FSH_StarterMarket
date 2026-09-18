namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed class TsetmcOptions
{
    public const string SectionName = "Tsetmc";

    public string MarketWatchUrl { get; set; } = default!;
}
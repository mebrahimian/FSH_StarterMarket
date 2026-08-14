namespace FSH.Modules.MarketIntelligence.Services.Codal.Configuration;

public sealed class CodalOptions
{
    public const string SectionName = "Codal";

    public string BaseUrl { get; init; } = string.Empty;
}
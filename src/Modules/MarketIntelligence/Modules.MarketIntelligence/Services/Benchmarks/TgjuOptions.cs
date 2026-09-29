namespace FSH.Modules.MarketIntelligence.Services.Benchmark;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by options binding.")]
internal sealed class TgjuOptions
{
    public const string SectionName = "Tgju";

    public string BaseUrl { get; set; } = string.Empty;
}
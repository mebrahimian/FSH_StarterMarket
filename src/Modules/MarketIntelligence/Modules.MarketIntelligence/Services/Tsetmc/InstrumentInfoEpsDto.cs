using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by System.Text.Json during deserialization.")]
internal sealed class InstrumentInfoEpsDto
{
    [JsonPropertyName("estimatedEPS")]
    public string? EstimatedEps { get; set; }

    [JsonPropertyName("sectorPE")]
    public decimal? SectorPE { get; set; }

    [JsonPropertyName("psr")]
    public decimal? SalesPerShare { get; set; }
}
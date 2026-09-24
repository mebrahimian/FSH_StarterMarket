using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by System.Text.Json during deserialization.")]
internal sealed class ClientTypeHistoryResponse
{
    [JsonPropertyName("clientType")]
    public List<ClientTypeHistoryDto> Items { get; set; } = [];
}
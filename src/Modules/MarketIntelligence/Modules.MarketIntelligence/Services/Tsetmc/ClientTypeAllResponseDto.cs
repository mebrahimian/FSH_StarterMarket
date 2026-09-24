using System.Text.Json.Serialization;

namespace Modules.MarketIntelligence.Services.Tsetmc;

public sealed class ClientTypeAllResponseDto
{
    [JsonPropertyName("clientTypeAllDto")]
    public IReadOnlyCollection<ClientTypeAllItemDto> Items { get; init; } = [];
}

public sealed class ClientTypeAllItemDto
{
    [JsonPropertyName("insCode")]
    public string InsCode { get; init; } = string.Empty;

    [JsonPropertyName("buy_I_Volume")]
    public decimal BuyIndividualVolume { get; init; }

    [JsonPropertyName("buy_CountI")]
    public decimal BuyIndividualCount { get; init; }

    [JsonPropertyName("sell_I_Volume")]
    public decimal SellIndividualVolume { get; init; }

    [JsonPropertyName("sell_CountI")]
    public decimal SellIndividualCount { get; init; }

    [JsonPropertyName("buy_N_Volume")]
    public decimal BuyInstitutionalVolume { get; init; }

    [JsonPropertyName("buy_CountN")]
    public decimal BuyInstitutionalCount { get; init; }

    [JsonPropertyName("sell_N_Volume")]
    public decimal SellInstitutionalVolume { get; init; }

    [JsonPropertyName("sell_CountN")]
    public decimal SellInstitutionalCount { get; init; }
}
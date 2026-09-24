using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by System.Text.Json during deserialization.")]
internal sealed class ClientTypeHistoryDto
{
    public int RecDate { get; set; }

    public string InsCode { get; set; } = string.Empty;

    [JsonPropertyName("buy_I_Volume")]
    public decimal BuyIndividualVolume { get; set; }

    [JsonPropertyName("buy_I_Value")]
    public decimal BuyIndividualValue { get; set; }

    [JsonPropertyName("buy_I_Count")]
    public decimal BuyIndividualCount { get; set; }

    [JsonPropertyName("sell_I_Volume")]
    public decimal SellIndividualVolume { get; set; }

    [JsonPropertyName("sell_I_Value")]
    public decimal SellIndividualValue { get; set; }

    [JsonPropertyName("sell_I_Count")]
    public decimal SellIndividualCount { get; set; }

    [JsonPropertyName("buy_N_Volume")]
    public decimal BuyInstitutionalVolume { get; set; }

    [JsonPropertyName("buy_N_Value")]
    public decimal BuyInstitutionalValue { get; set; }

    [JsonPropertyName("buy_N_Count")]
    public decimal BuyInstitutionalCount { get; set; }

    [JsonPropertyName("sell_N_Volume")]
    public decimal SellInstitutionalVolume { get; set; }

    [JsonPropertyName("sell_N_Value")]
    public decimal SellInstitutionalValue { get; set; }

    [JsonPropertyName("sell_N_Count")]
    public decimal SellInstitutionalCount { get; set; }
}
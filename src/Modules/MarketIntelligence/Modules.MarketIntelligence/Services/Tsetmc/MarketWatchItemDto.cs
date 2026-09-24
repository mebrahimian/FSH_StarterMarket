using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by System.Text.Json during deserialization.")]
internal sealed class MarketWatchItemDto
{
    [JsonPropertyName("pf")]
    public decimal? PriceFirst { get; set; }

    [JsonPropertyName("pmn")]
    public decimal? PriceMin { get; set; }

    [JsonPropertyName("pmx")]
    public decimal? PriceMax { get; set; }

    [JsonPropertyName("pcl")]
    public decimal? PClosing { get; set; }

    [JsonPropertyName("pdv")]
    public decimal? PDrCotVal { get; set; }

    [JsonPropertyName("py")]
    public decimal? PriceYesterday { get; set; }

    [JsonPropertyName("ztt")]
    public decimal? ZTotTran { get; set; }

    [JsonPropertyName("qtj")]
    public decimal? QTotTran5J { get; set; }

    [JsonPropertyName("qtc")]
    public decimal? QTotCap { get; set; }

    [JsonPropertyName("insCode")]
    public string InsCode { get; set; } = string.Empty;

    [JsonPropertyName("eps")]
    public decimal? Eps { get; set; }

    [JsonPropertyName("pe")]
    public string? PE { get; set; }

    [JsonPropertyName("ztd")]
    public decimal? SharesOutstanding { get; set; }

    [JsonPropertyName("bv")]
    public decimal? BaseVolume { get; set; }
}
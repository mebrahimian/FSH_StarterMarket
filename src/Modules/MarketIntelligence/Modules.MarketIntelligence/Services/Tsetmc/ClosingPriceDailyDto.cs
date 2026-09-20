using System.Diagnostics.CodeAnalysis;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by System.Text.Json during deserialization.")]
internal sealed class ClosingPriceDailyDto
{
    public string InsCode { get; set; } = string.Empty;

    public int DEven { get; set; }

    public int HEven { get; set; }

    public decimal PriceFirst { get; set; }

    public decimal PriceMin { get; set; }

    public decimal PriceMax { get; set; }

    public decimal PriceYesterday { get; set; }

    public decimal PClosing { get; set; }

    public decimal PDrCotVal { get; set; }

    public decimal ZTotTran { get; set; }

    public decimal QTotTran5J { get; set; }

    public decimal QTotCap { get; set; }
}
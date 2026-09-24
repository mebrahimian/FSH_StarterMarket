using System.Diagnostics.CodeAnalysis;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by System.Text.Json during deserialization.")]
internal sealed class InstrumentShareChangeDto
{
    public string InsCode { get; set; } = string.Empty;

    public int DEven { get; set; }

    public decimal NumberOfShareOld { get; set; }

    public decimal NumberOfShareNew { get; set; }
}
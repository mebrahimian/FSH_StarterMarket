using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by System.Text.Json during deserialization.")]
internal sealed class InstrumentInfoDto
{
    [JsonPropertyName("insCode")]
    public string InsCode { get; set; } = string.Empty;

    [JsonPropertyName("eps")]
    internal InstrumentInfoEpsDto? Eps { get; set; }
}
public sealed class InstrumentSearchResponseDto
{
    public IReadOnlyCollection<InstrumentSearchItemDto>
        InstrumentSearch
    { get; init; } = [];
}

public sealed class InstrumentSearchItemDto
{
    public string InsCode { get; init; } = string.Empty;

    public string LVal30 { get; init; } = string.Empty;

    public string LVal18AFC { get; init; } = string.Empty;
}

public sealed class InstrumentIdentityResponseDto
{
    public InstrumentIdentityDto? InstrumentIdentity { get; init; }
}

public sealed class InstrumentIdentityDto
{
    public string CIsin { get; init; } = string.Empty;

    public string LVal30 { get; init; } = string.Empty;

    public string LVal18AFC { get; init; } = string.Empty;

    public string YVal { get; init; } = string.Empty;
}
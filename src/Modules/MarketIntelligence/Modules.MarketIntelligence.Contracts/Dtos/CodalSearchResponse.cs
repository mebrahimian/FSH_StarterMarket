using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Contracts.Dtos;

public sealed class CodalSearchResponse
{
    public int Total { get; init; }

    [JsonPropertyName("Page")]
    public int TotalPages { get; init; }

    public Collection<CodalLetterDto> Letters { get; init; } = new();
}
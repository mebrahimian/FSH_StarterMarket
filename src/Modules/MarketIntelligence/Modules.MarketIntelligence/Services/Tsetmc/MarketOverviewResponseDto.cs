using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Modules.MarketIntelligence.Services.Tsetmc;

public sealed class MarketOverviewResponseDto
{
    [JsonPropertyName("marketOverview")]
    public MarketOverviewDto? MarketOverview { get; set; }
}

public sealed class MarketOverviewDto
{
    [JsonPropertyName("marketActivityDEven")]
    public int MarketActivityDEven { get; set; }
}
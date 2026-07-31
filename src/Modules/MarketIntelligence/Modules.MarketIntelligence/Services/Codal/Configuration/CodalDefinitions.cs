using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Configuration;


public sealed class CodalDefinitions
{
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<byte, CodalTableDefinition> MonthlyActivities
    {
        get;
    } = [];

}

public sealed class CodalTableDefinition
{
    public int MetaTableId { get; set; }

    public int MetaTableCode { get; set; }

    public Dictionary<string, int> SelectedCells { get; init; } = [];
}
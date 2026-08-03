using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
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
    public int MetaTableCode { get; set; }

    public bool UseColumnSum { get; init; }
    public Dictionary<string, int> SelectedCells { get; init; } = [];

    public Collection<CodalTableDefinition> AlternativeLayouts {get; init;} = [];
}

using System.Collections.ObjectModel;
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
    public int MetaTableCode { get; set; }

    public bool UseColumnSum { get; init; }

    public Dictionary<string, int> SelectedCells { get; init; } = [];

    public Collection<CodalTableDefinition> AlternativeLayouts { get; init; } = [];

    public Collection<CodalTableComponentDefinition> Components { get; init; } = [];
}

public sealed class CodalTableComponentDefinition
{
    public string Name { get; init; } = string.Empty;

    public int MetaTableCode { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CodalComponentOperation Operation { get; init; }

    public bool Required { get; init; }

    public bool HasTotalRow { get; init; } = true;

    public Dictionary<string, int> SelectedCells { get; init; } = [];
}

public enum CodalComponentOperation
{
    Add,
    Subtract
}
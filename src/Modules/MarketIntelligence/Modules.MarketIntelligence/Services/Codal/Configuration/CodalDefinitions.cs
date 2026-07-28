namespace FSH.Modules.MarketIntelligence.Services.Codal.Configuration;

public sealed class CodalDefinitions
{
    public ManufacturingMonthlySalesDefinition ManufacturingMonthlySales { get; set; } = new();
}

public sealed class ManufacturingMonthlySalesDefinition
{
    public int MetaTableId { get; set; }

    public int MetaTableCode { get; set; }

    public Dictionary<string, int> SelectedCells { get; init; } = new();
}
namespace FSH.Modules.MarketIntelligence.Services.Codal.Configuration;

public sealed class CodalDefinitions
{
    public CodalTableDefinition ManufacturingMonthlySales { get; set; } = new();
    public CodalTableDefinition RealEstateMonthlyActivity { get; set; } = new();

}

public sealed class CodalTableDefinition
{
    public int MetaTableId { get; set; }

    public int MetaTableCode { get; set; }

    public Dictionary<string, int> SelectedCells { get; init; } = [];
}
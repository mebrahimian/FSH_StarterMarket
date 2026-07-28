namespace FSH.Modules.MarketIntelligence.Services.Codal;

public static class CodalTableDefinitions
{
    public static bool IsManufacturingMonthlySalesTable(
        int metaTableId,
        int metaTableCode)
    {
        return metaTableId == 3465
            && metaTableCode == 1197;
    }
}
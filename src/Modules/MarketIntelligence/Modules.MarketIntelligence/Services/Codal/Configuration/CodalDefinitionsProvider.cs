using System.Reflection;
using System.Text.Json;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Configuration;

public static class CodalDefinitionsProvider
{
    public static CodalDefinitions Load()
    {
        var assemblyPath = Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location)!;

        var path = Path.Combine(
            assemblyPath,
            "Services",
            "Codal",
            "Configuration",
            "CodalDefinitions.json");

        var json = File.ReadAllText(path);
        var ret = JsonSerializer.Deserialize<CodalDefinitions>(json) ?? 
            throw new InvalidOperationException(
        "CodalDefinitions.json could not be deserialized."); 
        Console.WriteLine(ret.RealEstateMonthlyActivity.GetType().FullName);
        Console.WriteLine(ret.RealEstateMonthlyActivity.MetaTableId);
        Console.WriteLine(ret.RealEstateMonthlyActivity.MetaTableCode);
        Console.WriteLine(ret.RealEstateMonthlyActivity.SelectedCells.Count);
        return ret;
    }
}
using System.Reflection;
using System.Text.Json;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Configuration;

public static class CodalDefinitionsProvider
{
    private static readonly CodalDefinitions Definitions = LoadFromFile();

    public static CodalDefinitions Load()
    {
        return Definitions;
    }

    private static CodalDefinitions LoadFromFile()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        var path = Path.Combine(
            assemblyPath,
            "Services",
            "Codal",
            "Configuration",
            "CodalDefinitions.json");

        System.Diagnostics.Debug.WriteLine(
    $"Codal definitions path: {path}");
        var json = File.ReadAllText(path);
    
    return JsonSerializer.Deserialize<CodalDefinitions>(json)
            ?? throw new InvalidOperationException(
                "CodalDefinitions.json could not be deserialized.");
    }
}
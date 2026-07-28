using System;
using System.IO;
using FSH.MarketIntelligence.TemplateDiscovery.Services;

Console.WriteLine("=== Codal Template Discovery ===");

Console.Write("JSON file path: ");

var file = Console.ReadLine();

if (string.IsNullOrWhiteSpace(file))
{
    Console.WriteLine("No file selected.");
    return;
}

if (!File.Exists(file))
{
    Console.WriteLine("File not found.");
    return;
}

string json = File.ReadAllText(file);

var service = new CodalTemplateDiscoveryService();

var result = service.Extract(json);

Console.WriteLine();
Console.WriteLine($"Message : {result.Message}");
Console.WriteLine($"Length  : {result.Length:N0}");
Console.WriteLine("Done.");
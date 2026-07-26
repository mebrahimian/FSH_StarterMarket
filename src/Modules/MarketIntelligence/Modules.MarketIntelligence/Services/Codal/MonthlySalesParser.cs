using System.Net.Http;
using HtmlAgilityPack;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class MonthlySalesParser : IMonthlySalesParser
{
    private readonly HttpClient _httpClient;

    public MonthlySalesParser(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MonthlySalesParseResult?> ParseAsync(
        string url,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);
        var fullUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"https://codal.ir" + url;
        var html = await _httpClient.GetStringAsync(
        new Uri(fullUrl),
        cancellationToken);

        var match = Regex.Match(html, @"var\s+datasource\s*=\s*(\{.*?\});",RegexOptions.Singleline);

        if (!match.Success)
            return null;

        var datasourceJson = match.Groups[1].Value;

        Console.WriteLine(datasourceJson[..500]);

        using var document = JsonDocument.Parse(datasourceJson);
        var root = document.RootElement;

        Console.WriteLine(root.GetProperty("title_En").GetString());

        var sheets = root.GetProperty("sheets");

        Console.WriteLine(sheets.GetArrayLength());

        var tables = sheets[0].GetProperty("tables");

        Console.WriteLine(tables.GetArrayLength());

        foreach (var table in tables.EnumerateArray())
        {
            Console.WriteLine(table.GetProperty("aliasName").GetString());
        }
        // فعلاً فقط تست
        return null;
    }
}
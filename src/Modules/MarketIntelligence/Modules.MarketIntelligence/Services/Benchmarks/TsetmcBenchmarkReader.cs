using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Benchmark;

public sealed class TsetmcBenchmarkReader(HttpClient httpClient)
{
    private const string HistoryBaseUrl = $"https://cdn.tsetmc.com/api/Index/GetIndexB2History";

    public async Task<IReadOnlyCollection<TsetmcBenchmarkPriceRow>> GetHistoryAsync(string insCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            insCode);

        string url = $"{HistoryBaseUrl}/{insCode}";

        TsetmcIndexHistoryResponse? response =
            await httpClient
                .GetFromJsonAsync<TsetmcIndexHistoryResponse>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        if (response?.IndexB2 is null ||
            response.IndexB2.Count == 0)
        {
            return [];
        }

        return response.IndexB2
            .Select(x =>
                new TsetmcBenchmarkPriceRow(
                    ToDate(x.DEven),
                    x.XNivInuClMresIbs))
            .OrderBy(x => x.TradeDate)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TsetmcBenchmarkCurrentRow>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        Uri tseUrl = new("https://cdn.tsetmc.com/api/Index/GetIndexB1LastAll/SelectedIndexes/1");

        Uri ifbUrl = new("https://cdn.tsetmc.com/api/Index/GetIndexB1LastAll/SelectedIndexes/2");

        TsetmcCurrentResponse? tseResponse =
            await httpClient
                .GetFromJsonAsync<TsetmcCurrentResponse>(
                    tseUrl,
                    cancellationToken)
                .ConfigureAwait(false);

        TsetmcCurrentResponse? ifbResponse =
            await httpClient
                .GetFromJsonAsync<TsetmcCurrentResponse>(
                    ifbUrl,
                    cancellationToken)
                .ConfigureAwait(false);

        List<TsetmcCurrentItem> items = [];

        if (tseResponse?.IndexB1 is not null)
        {
            items.AddRange(tseResponse.IndexB1);
        }

        if (ifbResponse?.IndexB1 is not null)
        {
            items.AddRange(ifbResponse.IndexB1);
        }

        return items
    .Select(x =>
        new TsetmcBenchmarkCurrentRow(
            x.InsCode,
            x.HEven,
            x.CurrentValue,
            x.LowValue,
            x.HighValue))
    .ToArray();
    }

    private static DateOnly ToDate(long value)
    {
        return DateOnly.ParseExact(
            value.ToString(
                CultureInfo.InvariantCulture),
            "yyyyMMdd",
            CultureInfo.InvariantCulture);
    }

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialization.")]
    private sealed record TsetmcIndexHistoryResponse(
        [property: JsonPropertyName("indexB2")]
        List<TsetmcIndexHistoryItem> IndexB2);

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialization.")]
    private sealed record TsetmcIndexHistoryItem(
        [property: JsonPropertyName("dEven")]
        long DEven,

        [property: JsonPropertyName("xNivInuClMresIbs")]
        decimal XNivInuClMresIbs);

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialization.")]
    private sealed record TsetmcCurrentResponse(
        [property: JsonPropertyName("indexB1")]
        List<TsetmcCurrentItem> IndexB1);

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialization.")]
    private sealed record TsetmcCurrentItem(
    [property: JsonPropertyName("insCode")]
    string InsCode,

    [property: JsonPropertyName("hEven")]
    int HEven,

    [property: JsonPropertyName("xDrNivJIdx004")]
    decimal CurrentValue,

    [property: JsonPropertyName("xPhNivJIdx004")]
    decimal HighValue,

    [property: JsonPropertyName("xPbNivJIdx004")]
    decimal LowValue);
}

public sealed record TsetmcBenchmarkPriceRow(
    DateOnly TradeDate,
    decimal ClosePrice);

public sealed record TsetmcBenchmarkCurrentRow(
    string InsCode,
    int HEven,
    decimal CurrentValue,
    decimal LowValue,
    decimal HighValue);
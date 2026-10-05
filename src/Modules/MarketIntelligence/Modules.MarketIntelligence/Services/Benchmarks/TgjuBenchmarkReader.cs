using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSH.Modules.MarketIntelligence.Services.Benchmark;
#pragma warning disable CA1812
public sealed class TgjuBenchmarkReader(
    HttpClient httpClient)
{

    private const string HistoryBaseUrl = $"https://api.tgju.org/v1/market/indicator/summary-table-data";
    private const string CurrentBaseUrl = $"https://api.tgju.org/v1/widget/tmp";
    public async Task<TgjuBenchmarkPriceRow?> GetCurrentAsync(
    string externalCode,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            externalCode);

        string url =
            $"{CurrentBaseUrl}?keys={Uri.EscapeDataString(externalCode)}";

        TgjuWidgetResponse? response =
            await httpClient
                .GetFromJsonAsync<TgjuWidgetResponse>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        TgjuWidgetIndicator? indicator =
            response?.Response?.Indicators?
                .FirstOrDefault();

        if (indicator is null)
        {
            return null;
        }

        DateTime updatedAt =
            DateTime.ParseExact(
                indicator.UpdatedAt,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture);

        bool isTether =
            string.Equals(
                externalCode,
                "crypto-tether",
                StringComparison.OrdinalIgnoreCase);

        if (isTether)
        {
            if (indicator.PriceIrr.ValueKind is
                JsonValueKind.Undefined or
                JsonValueKind.Null)
            {
                return null;
            }

            decimal priceIrr = ParseDecimal(indicator.PriceIrr);

            return new TgjuBenchmarkPriceRow(
                DateOnly.FromDateTime(updatedAt),
                priceIrr,
                priceIrr,
                priceIrr,
                priceIrr);
        }

        return new TgjuBenchmarkPriceRow(
            DateOnly.FromDateTime(updatedAt),
            ParseDecimal(indicator.Open),
            ParseDecimal(indicator.Low),
            ParseDecimal(indicator.High),
            ParseDecimal(indicator.Price));
    }
    public async Task<IReadOnlyCollection<TgjuBenchmarkPriceRow>> GetHistoryAsync(string externalCode, DateOnly fromDate, CancellationToken cancellationToken)
    {
        const int pageSize = 500;

        List<TgjuBenchmarkPriceRow> result = [];

        int start = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string url =
                $"{HistoryBaseUrl}/{externalCode}" +
                $"?lang=fa" +
                $"&start={start}" +
                $"&length={pageSize}" +
                $"&convert_to_ad=1";

            TgjuHistoryResponse? response =
                await httpClient
                    .GetFromJsonAsync<TgjuHistoryResponse>(
                        url,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (response is null ||
                response.Data.Count == 0)
            {
                break;
            }

            foreach (List<string> row in response.Data)
            {
                if (row.Count < 7)
                {
                    continue;
                }

                DateOnly tradeDate =
                    DateOnly.ParseExact(
                        row[6],
                        "yyyy/MM/dd",
                        CultureInfo.InvariantCulture);

                if (tradeDate < fromDate)
                {
                    continue;
                }

                result.Add(
                    new TgjuBenchmarkPriceRow(
                        tradeDate,
                        ParseDecimal(row[0]),
                        ParseDecimal(row[1]),
                        ParseDecimal(row[2]),
                        ParseDecimal(row[3])));
            }

            start += response.Data.Count;

            if (start >= response.RecordsTotal)
            {
                break;
            }
        }

        return result;
    }

    private static decimal ParseDecimal(string value) =>
     decimal.Parse(
         value.Replace(
             ",",
             string.Empty,
             StringComparison.Ordinal),
         CultureInfo.InvariantCulture);
    private static decimal ParseDecimal(JsonElement value) =>
    value.ValueKind switch
    {
        JsonValueKind.Number =>
            value.GetDecimal(),

        JsonValueKind.String =>
            ParseDecimal(value.GetString() ?? "0"),

        _ => 0m,
    };
    private sealed record TgjuWidgetResponse(
    [property: JsonPropertyName("response")]
    TgjuWidgetPayload? Response);

    private sealed record TgjuWidgetPayload(
        [property: JsonPropertyName("indicators")]
    List<TgjuWidgetIndicator>? Indicators);

    private sealed record TgjuWidgetIndicator(
        [property: JsonPropertyName("p")]
    JsonElement Price,

        [property: JsonPropertyName("p_irr")]
    JsonElement PriceIrr,

        [property: JsonPropertyName("o")]
    JsonElement Open,

        [property: JsonPropertyName("h")]
    JsonElement High,

        [property: JsonPropertyName("l")]
    JsonElement Low,

        [property: JsonPropertyName("updated_at")]
    string UpdatedAt);
    private sealed record TgjuHistoryResponse(
    int RecordsTotal,
    List<List<string>> Data);
}

public sealed record TgjuBenchmarkPriceRow(
    DateOnly TradeDate,
    decimal OpenPrice,
    decimal LowPrice,
    decimal HighPrice,
    decimal ClosePrice);
#pragma warning restore CA1812
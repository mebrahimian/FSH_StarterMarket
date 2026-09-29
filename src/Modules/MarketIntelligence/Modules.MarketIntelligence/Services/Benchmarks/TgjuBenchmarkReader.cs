using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;

namespace FSH.Modules.MarketIntelligence.Services.Benchmark;
#pragma warning disable CA1812
internal sealed class TgjuBenchmarkReader(
    HttpClient httpClient)
{

    private const string BaseUrl =  $"https://api.tgju.org/v1/market/indicator/summary-table-data";

    public async Task<IReadOnlyCollection<TgjuBenchmarkPriceRow>>
        GetHistoryAsync(
            string externalCode,
            DateOnly fromDate,
            CancellationToken cancellationToken)
    {
        const int pageSize = 500;

        List<TgjuBenchmarkPriceRow> result = [];

        int start = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string url =
                $"{BaseUrl}/{externalCode}" +
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

    private sealed record TgjuHistoryResponse(
    int RecordsTotal,
    List<List<string>> Data);
}

internal sealed record TgjuBenchmarkPriceRow(
    DateOnly TradeDate,
    decimal OpenPrice,
    decimal LowPrice,
    decimal HighPrice,
    decimal ClosePrice);
#pragma warning restore CA1812
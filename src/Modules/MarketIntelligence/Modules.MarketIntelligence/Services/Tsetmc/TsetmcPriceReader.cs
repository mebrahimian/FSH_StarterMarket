
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed class TsetmcPriceReader(
    HttpClient httpClient,
    IOptions<TsetmcOptions> options)
{
    private readonly TsetmcOptions _options = options.Value;

    internal async Task<IReadOnlyCollection<ClosingPriceDailyDto>>
        GetHistoryAsync(
            string insCode,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(insCode);

        string url =
            $"{_options.BaseUrl}" +
            $"{_options.ClosingPriceHistoryPath}/" +
            $"{insCode}/0";

        ClosingPriceDailyResponse? response =
            await httpClient
                .GetFromJsonAsync<ClosingPriceDailyResponse>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.Items ?? [];
    }
}
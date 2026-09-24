
using Microsoft.Extensions.Options;
using Modules.MarketIntelligence.Services.Tsetmc;
using System.Globalization;
using System.Net.Http.Json;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed class TsetmcPriceReader(HttpClient httpClient, IOptions<TsetmcOptions> options)
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
    internal async Task<IReadOnlyCollection<ClientTypeHistoryDto>>
    GetClientTypeHistoryAsync(string insCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(insCode);

        string url =
            $"{_options.BaseUrl}" +
            $"{_options.ClientTypeHistoryPath}/" +
            insCode;

        ClientTypeHistoryResponse? response =
            await httpClient
                .GetFromJsonAsync<ClientTypeHistoryResponse>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.Items ?? [];
    }
    internal async Task<IReadOnlyCollection<InstrumentShareChangeDto>>
    GetShareChangesAsync(string insCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(insCode);

        string url =
            $"{_options.BaseUrl}" +
            $"{_options.ShareChangePath}/" +
            insCode;

        InstrumentShareChangeResponse? response =
            await httpClient
                .GetFromJsonAsync<InstrumentShareChangeResponse>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.Items ?? [];
    }
    internal async Task<IReadOnlyCollection<MarketWatchItemDto>>
    GetMarketWatchAsync(CancellationToken cancellationToken)
    {
        string url =
            $"{_options.BaseUrl}" +
            _options.MarketWatchPath;

        MarketWatchResponse? response =
            await httpClient
                .GetFromJsonAsync<MarketWatchResponse>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.Items ?? [];
    }
    internal async Task<IReadOnlyCollection<InstrumentSearchItemDto>>
    GetInstrumentSearchAsync(string symbol, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        string encodedSymbol =
            Uri.EscapeDataString(symbol);

        string path =
            string.Format(
                CultureInfo.InvariantCulture,
                _options.InstrumentSearchPath,
                encodedSymbol);

        string url =
            $"{_options.BaseUrl}{path}";

        InstrumentSearchResponseDto? response =
            await httpClient
                .GetFromJsonAsync<InstrumentSearchResponseDto>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.InstrumentSearch ?? [];
    }
    internal async Task<InstrumentIdentityDto?>
    GetInstrumentIdentityAsync(string insCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(insCode);

        string path =
            string.Format(
                CultureInfo.InvariantCulture,
                _options.InstrumentIdentityPath,
                insCode);

        string url =
            $"{_options.BaseUrl}{path}";

        InstrumentIdentityResponseDto? response =
            await httpClient
                .GetFromJsonAsync<InstrumentIdentityResponseDto>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.InstrumentIdentity;
    }
    internal async Task<InstrumentInfoDto?>
    GetInstrumentInfoAsync(string insCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(insCode);

        string url =
            $"{_options.BaseUrl}" +
            $"{_options.InstrumentInfoPath}/" +
            insCode;

        InstrumentInfoResponse? response =
            await httpClient
                .GetFromJsonAsync<InstrumentInfoResponse>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.Item;
    }
    internal async Task<MarketOverviewDto?>
    GetMarketOverviewAsync(
        CancellationToken cancellationToken)
    {
        string url =
            $"{_options.BaseUrl}" +
            _options.MarketOverviewPath;

        MarketOverviewResponseDto? response =
            await httpClient
                .GetFromJsonAsync<MarketOverviewResponseDto>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.MarketOverview;
    }
    internal async Task<IReadOnlyCollection<ClientTypeAllItemDto>>
    GetClientTypeAllAsync(
        CancellationToken cancellationToken)
    {
        string url =
            $"{_options.BaseUrl}" +
            _options.ClientTypeAllPath;

        ClientTypeAllResponseDto? response =
            await httpClient
                .GetFromJsonAsync<ClientTypeAllResponseDto>(
                    url,
                    cancellationToken)
                .ConfigureAwait(false);

        return response?.Items ?? [];
    }

}
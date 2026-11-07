
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CodalReportHtmlDownloader(
    HttpClient httpClient,
    IOptions<CodalOptions> options,
    CodalRequestGate requestGate)
{
    public async Task<string> DownloadAsync(
        string disclosureUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(disclosureUrl);

        Uri reportUri = Uri.TryCreate(
            disclosureUrl,
            UriKind.Absolute,
            out Uri? absoluteUri)
            ? absoluteUri
            : new Uri(
                new Uri(options.Value.BaseUrl),
                disclosureUrl);

        const int maxAttempts = 3;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using HttpResponseMessage response =
                    await requestGate.ExecuteAsync(
                        async ct =>
                        {
                            using var request =
                                new HttpRequestMessage(
                                    HttpMethod.Get,
                                    reportUri);

                            request.Version = HttpVersion.Version11;
                            request.VersionPolicy =
                                HttpVersionPolicy.RequestVersionOrLower;

                            return await httpClient.SendAsync(
                                request,
                                HttpCompletionOption.ResponseHeadersRead,
                                ct);
                        },
                        cancellationToken);

                if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                    (int)response.StatusCode == 490)
                {
                    throw new HttpRequestException(
                        "Codal rate limit or security verification triggered.",
                        null,
                        response.StatusCode);
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode == HttpStatusCode.TooManyRequests ||
                      (int?)ex.StatusCode == 490)
            {
                // Do not retry security blocks.
                throw;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
            }
            catch (IOException ex)
            {
                lastException = ex;
            }
            catch (OperationCanceledException ex)
            {
                lastException = ex;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(attempt * 5),
                    cancellationToken);
            }
        }

        throw new HttpRequestException(
            $"Downloading Codal report failed after {maxAttempts} attempts. Url: {reportUri}",
            lastException);
    }
}

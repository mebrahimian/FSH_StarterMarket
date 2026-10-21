using FSH.Modules.MarketIntelligence.Services.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc.Jobs;

public sealed class TsetmcBackgroundJob(
    PriceHistoryCollectorService priceHistoryCollector,
    TsetmcShareChangeCollectorService shareChangeCollector,
    BackgroundJobStatusService jobStatusService)
{
    public async Task RunIncrementalAsync(CancellationToken cancellationToken)
    {
        await jobStatusService.ExecuteAsync(
            "tsetmc-daily-price",
            "TSETMC Daily Price",
            async () =>
            {
                await priceHistoryCollector
                    .RunIncrementalAsync(cancellationToken)
                    .ConfigureAwait(false);
            },
            cancellationToken)
            .ConfigureAwait(false);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task RunPriceIncrementalWithShareChangesAsync(
    CancellationToken cancellationToken)
    {
        await jobStatusService.ExecuteAsync(
            "tsetmc-daily-price-share-changes",
            "TSETMC Daily Price + Share Changes",
            async () =>
            {
                // 1. Collect daily prices and valuations
                await priceHistoryCollector
                    .RunIncrementalAsync(cancellationToken)
                    .ConfigureAwait(false);

                // 2. Collect historical share changes
                await shareChangeCollector
                    .CollectAsync(cancellationToken)
                    .ConfigureAwait(false);
            },
            cancellationToken)
            .ConfigureAwait(false);
    }
    [AutomaticRetry(Attempts = 0)]
    public async Task RunShareChangesAsync(
    CancellationToken cancellationToken)
    {
        await jobStatusService.ExecuteAsync(
            "tsetmc-share-changes",
            "TSETMC Share Changes",
            async () =>
            {
                await shareChangeCollector
                    .CollectAsync(cancellationToken)
                    .ConfigureAwait(false);
            },
            cancellationToken)
            .ConfigureAwait(false);
    }
}
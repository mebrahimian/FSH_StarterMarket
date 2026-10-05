using FSH.Modules.MarketIntelligence.Services.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc.Jobs;

public sealed class TsetmcBackgroundJob(
    PriceHistoryCollectorService priceHistoryCollector,
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
    public async Task RunPriceIncrementalWithShareChangesAsync(CancellationToken cancellationToken)
    {
        await jobStatusService.ExecuteAsync(
            "tsetmc-daily-price-share-changes",
            "TSETMC Daily Price + Share Changes",
            async () =>
            {
                await priceHistoryCollector
                    .RunIncrementalAsync(
                        cancellationToken,
                        includeShareChanges: true)
                    .ConfigureAwait(false);
            },
            cancellationToken)
            .ConfigureAwait(false);
    }
}
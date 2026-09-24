using Hangfire;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc.Jobs;

public sealed class TsetmcBackgroundJob(
    PriceHistoryCollectorService priceHistoryCollector,
    ILogger<TsetmcBackgroundJob> logger)
{
    public async Task RunIncrementalAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("TSETMC incremental job started.");

        int processed =
            await priceHistoryCollector
                .RunIncrementalAsync(cancellationToken)
                .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
            "TSETMC incremental job completed. Processed={Processed}",
            processed);
        }

        
    }
        
    [AutomaticRetry(Attempts = 0)]
    public Task RunPriceIncrementalWithShareChangesAsync(
        CancellationToken cancellationToken)
    {
        return priceHistoryCollector
            .RunIncrementalAsync(
                cancellationToken,
                includeShareChanges: true);
    }
}
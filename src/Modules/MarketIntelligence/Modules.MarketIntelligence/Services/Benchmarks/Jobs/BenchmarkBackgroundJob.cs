using FSH.Modules.MarketIntelligence.Services.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Benchmark.Jobs;

public sealed class BenchmarkBackgroundJob(
    BenchmarkPriceCollectorService collectorService,
    BackgroundJobStatusService jobStatusService,
    ILogger<BenchmarkBackgroundJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task RunTgjuIncrementalAsync(
    CancellationToken cancellationToken)
    {
        await jobStatusService.ExecuteAsync(
            "benchmark-tgju",
            "TGJU Benchmark",
            async () =>
            {
                logger.LogInformation(
                    "TGJU benchmark incremental job started.");

                await collectorService
                    .SyncTgjuIncrementalAsync(cancellationToken)
                    .ConfigureAwait(false);

                logger.LogInformation(
                    "TGJU benchmark incremental job completed.");
            },
            cancellationToken)
            .ConfigureAwait(false);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task RunTsetmcIndexIncrementalAsync(
    CancellationToken cancellationToken)
    {
        await jobStatusService.ExecuteAsync(
            "benchmark-tsetmc-index",
            "TSETMC Benchmark Index",
            async () =>
            {
                logger.LogInformation(
                    "TSETMC benchmark index incremental job started.");

                BenchmarkSyncResult result =
                    await collectorService
                        .SyncTsetmcIncrementalAsync(
                            cancellationToken)
                        .ConfigureAwait(false);

                if (logger.IsEnabled(LogLevel.Information))
                { 
                    logger.LogInformation(
                        "TSETMC benchmark index incremental job completed. AssetsFound={AssetsFound}, AssetsProcessed={AssetsProcessed}, PricesInserted={PricesInserted}",
                        result.AssetsFound,
                        result.AssetsProcessed,
                        result.PricesInserted);
                }
            },
            cancellationToken)
            .ConfigureAwait(false);
    }
}
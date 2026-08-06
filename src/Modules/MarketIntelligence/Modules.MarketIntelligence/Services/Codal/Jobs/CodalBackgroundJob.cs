using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Jobs;

public sealed class CodalBackgroundJob(
    ICodalCollectorService collectorService)
{
    public Task RunIncrementalAsync(
    CancellationToken cancellationToken)
    {
        return collectorService
            .CollectIncrementalAsync(
                cancellationToken);
    }

    public Task RunBackfillAsync(
        CancellationToken cancellationToken)
    {
        return collectorService
            .CollectBackfillAsync(
                cancellationToken);
    }

    public Task RunParsePendingAsync(
        CancellationToken cancellationToken)
    {
        return collectorService
            .ParsePendingDisclosuresAsync(
                cancellationToken);
    }
}
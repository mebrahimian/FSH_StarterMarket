using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Jobs;

public sealed class CodalBackgroundJob(
    ICodalCollectorService collectorService)
{
    public Task RunIncrementalAsync()
    {
        return collectorService
            .CollectIncrementalAsync(
                CancellationToken.None);
    }

    public Task RunBackfillAsync()
    {
        return collectorService
            .CollectBackfillAsync(
                CancellationToken.None);
    }

    public Task RunParsePendingAsync()
    {
        return collectorService
            .ParsePendingDisclosuresAsync(
                CancellationToken.None);
    }
}
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using Hangfire;

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

    public Task RunBackfillAsync(CancellationToken cancellationToken)
    {
        return collectorService
            .CollectBackfillAsync(cancellationToken);
    }
    [AutomaticRetry(Attempts = 0)]
    public Task RunSymbolBackfillAsync(string symbol, string fromDate, string toDate)
    {
        return collectorService
            .CollectSymbolBackfillAsync(symbol, fromDate, toDate, CancellationToken.None);
    }

    public Task RunParsePendingAsync(CancellationToken cancellationToken)
    {
        return collectorService
            .ParsePendingDisclosuresAsync(cancellationToken);
    }
}
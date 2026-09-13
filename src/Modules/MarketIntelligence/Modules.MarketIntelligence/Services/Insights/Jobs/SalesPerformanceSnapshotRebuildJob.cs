using FSH.Framework.Jobs.Services;
using Hangfire;

namespace FSH.Modules.MarketIntelligence.Services.Insights.Jobs;

public sealed class SalesPerformanceSnapshotRebuildJob(
    SalesPerformanceSnapshotRebuildService rebuildService,
    IJobService jobService)
{
    private const int CompanyBatchSize = 10;

    [AutomaticRetry(Attempts = 0)]
    public async Task RunMissingAsync(
        int afterCompanyId,
        CancellationToken cancellationToken)
    {
        int? nextAfterCompanyId =
            await rebuildService.RebuildMissingAsync(
                afterCompanyId,
                CompanyBatchSize,
                cancellationToken);

        if (!nextAfterCompanyId.HasValue)
        {
            return;
        }

        jobService.Enqueue<SalesPerformanceSnapshotRebuildJob>(
            job => job.RunMissingAsync(
                nextAfterCompanyId.Value,
                CancellationToken.None));
    }
    [AutomaticRetry(Attempts = 0)]
    public async Task RunMissingBatchAsync(
    int afterCompanyId,
    CancellationToken cancellationToken)
    {
        await rebuildService.RebuildMissingAsync(
            afterCompanyId,
            CompanyBatchSize,
            cancellationToken);
    }
}
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Insights;

public sealed class SalesPerformanceSnapshotRebuildService(
    MarketIntelligenceDbContext dbContext,
    SalesPerformanceSnapshotService snapshotService)
{
    public async Task<int?> RebuildMissingAsync(
    int afterCompanyId,
    int companyBatchSize,
    CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(afterCompanyId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyBatchSize);

        List<int> companyIds =
    await dbContext.MonthlyActivitySummaries
        .AsNoTracking()
        .Where(x =>
            x.CompanyId.HasValue &&
            x.CompanyId.Value > afterCompanyId &&
            !dbContext.SalesPerformanceSnapshots
                .Any(snapshot =>
                    snapshot.CompanyId == x.CompanyId.Value &&
                    snapshot.PeriodEndDate == x.PeriodEndDate))
        .Select(x => x.CompanyId!.Value)
        .Distinct()
        .OrderBy(x => x)
        .Take(companyBatchSize)
        .ToListAsync(cancellationToken);

        if (companyIds.Count == 0)
        {
            return null;
        }
        foreach (int companyId in companyIds)
        {
            List<string> periods =
                await dbContext.MonthlyActivitySummaries
                    .AsNoTracking()
                    .Where(x =>
                        x.CompanyId == companyId &&
                        !dbContext.SalesPerformanceSnapshots
                            .Any(snapshot =>
                                snapshot.CompanyId == companyId &&
                                snapshot.PeriodEndDate == x.PeriodEndDate))
                    .Select(x => x.PeriodEndDate)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync(cancellationToken);

            foreach (string periodEndDate in periods)
            {
                await snapshotService.RebuildAsync(
                    companyId,
                    periodEndDate,
                    cancellationToken);

                dbContext.ChangeTracker.Clear();
            }
        }
        return companyIds[^1];
    }
}
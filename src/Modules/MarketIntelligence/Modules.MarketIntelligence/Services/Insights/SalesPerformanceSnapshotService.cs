using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain.Insights;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Insights;

public sealed class SalesPerformanceSnapshotService(
    MarketIntelligenceDbContext dbContext,
    SalesPerformanceAnalyzer analyzer)
{
    public async Task<SalesPerformanceSnapshot?> RebuildAsync(
        int companyId,
        string periodEndDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndDate);

        SalesPerformanceMetrics? metrics =
            await analyzer.AnalyzeAsync(
                companyId,
                periodEndDate,
                cancellationToken);

        if (metrics is null)
        {
            return null;
        }

        string? symbol =
            await dbContext.MonthlyActivitySummaries
                .AsNoTracking()
                .Where(x =>
                    x.CompanyId == companyId &&
                    x.PeriodEndDate == periodEndDate)
                .OrderByDescending(x => x.PublishDateTime)
                .Select(x => x.Symbol)
                .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        SalesPerformanceSnapshot? snapshot =
            await dbContext.SalesPerformanceSnapshots
                .SingleOrDefaultAsync(
                    x =>
                        x.CompanyId == companyId &&
                        x.PeriodEndDate == periodEndDate,
                    cancellationToken);

        if (snapshot is null)
        {
            snapshot =
                new SalesPerformanceSnapshot(
                    companyId,
                    symbol,
                    periodEndDate,
                    metrics);

            await dbContext.SalesPerformanceSnapshots.AddAsync(
                snapshot,
                cancellationToken);
        }
        else
        {
            snapshot.Update(
                symbol,
                metrics);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return snapshot;
    }
}
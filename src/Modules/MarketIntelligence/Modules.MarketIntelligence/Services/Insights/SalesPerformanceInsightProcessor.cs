using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Insights;

public sealed class SalesPerformanceInsightProcessor(
    MarketIntelligenceDbContext dbContext,
    SalesPerformanceSnapshotService snapshotService)
    : IDisclosureInsightProcessor
{
    public bool CanProcess(Disclosure disclosure)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        return
            disclosure.Let == 58 ||
            (disclosure.Rt == 2 &&
             disclosure.Let == 8);
    }

    public async Task ProcessAsync(
        Disclosure disclosure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        var summary =
            await dbContext.MonthlyActivitySummaries
                .AsNoTracking()
                .Where(x =>
                    x.DisclosureId == disclosure.Id &&
                    x.CompanyId != null)
                .OrderByDescending(x => x.PublishDateTime)
                .Select(x => new
                {
                    CompanyId = x.CompanyId!.Value,
                    x.PeriodEndDate,
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (summary is null ||
            string.IsNullOrWhiteSpace(summary.PeriodEndDate))
        {
            return;
        }

        await snapshotService.RebuildAsync(
            summary.CompanyId,
            summary.PeriodEndDate,
            cancellationToken);
    }
}
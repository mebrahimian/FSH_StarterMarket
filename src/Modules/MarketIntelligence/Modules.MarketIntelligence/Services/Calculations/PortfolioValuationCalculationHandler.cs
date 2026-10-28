using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Services.Portfolio;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed class PortfolioValuationCalculationHandler(
    MarketIntelligenceDbContext dbContext,
    PortfolioValuationIncrementalService incrementalService,
    PortfolioValuationBackfillService backfillService)
    : ICalculationTaskHandler
{
    public string TaskType => "PortfolioValuation";

    public async Task ExecuteAsync(
        int companyId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);

        DateTimeOffset iranNow =
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTimeOffset.UtcNow,
                "Iran Standard Time");

        DateOnly evaluationDate =
            DateOnly.FromDateTime(iranNow.DateTime);

        bool hasSnapshot =
            await dbContext.PortfolioValuationSnapshots
                .AsNoTracking()
                .AnyAsync(
                    x => x.CompanyId == companyId,
                    cancellationToken);
        if (!hasSnapshot)
        {
            await backfillService.RunAsync(
                evaluationDate,
                companyId,
                cancellationToken);

            bool snapshotCreated =
                await dbContext.PortfolioValuationSnapshots
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.CompanyId == companyId,
                        cancellationToken);

            if (!snapshotCreated)
            {
                throw new InvalidOperationException(
                    $"Portfolio valuation backfill produced no snapshots. " +
                    $"CompanyId={companyId}");
            }

            return;
        }

        await incrementalService.RunAsync(
            evaluationDate,
            companyId,
            cancellationToken);
    }
}
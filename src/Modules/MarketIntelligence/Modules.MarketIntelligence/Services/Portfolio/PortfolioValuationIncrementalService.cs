
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using FSH.Modules.MarketIntelligence.Services.Calculations;
using FSH.Modules.MarketIntelligence.Domain.Enums;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed class PortfolioValuationIncrementalService(
    MarketIntelligenceDbContext dbContext,
    PortfolioValuationReportCalculatorService calculatorService,
    PortfolioValuationSnapshotService snapshotService)
{
    public async Task<int> RunAsync(
    DateOnly evaluationDate,
    int companyId,
    CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);

        var revisions =
            await PortfolioValuationReportSelector
                .GetReportRevisionsAsync(
                    dbContext,
                    cancellationToken);

        return await RunAsync(
            evaluationDate,
            companyId,
            revisions,
            cancellationToken);
    }

    public async Task<int> RunAsync(
        DateOnly evaluationDate,
        int companyId,
        IReadOnlyList<PortfolioValuationReportRef> revisions,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        ArgumentNullException.ThrowIfNull(revisions);

        var periods = revisions
                .Where(r =>
                r.CompanyId == companyId &&
                r.PublishDateTime.HasValue &&
                DateOnly.FromDateTime(
                    r.PublishDateTime.Value) <= evaluationDate)
            .GroupBy(r => r.PeriodEndDate)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new
            {
                PeriodEndDate = g.Key,
                FirstPublishedAt = g.Min(
                    r => r.PublishDateTime!.Value),
                Revisions = g.ToArray()
            })
            .ToArray();

        if (periods.Length == 0)
        {
            return 0;
        }

        var strategy =
            dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(
                    cancellationToken);
            await CalculationCompanyLock.AcquireAsync(
                dbContext,
                companyId,
                "PortfolioValuation",
                cancellationToken);
            var active = await dbContext.PortfolioValuationSnapshots
                .Where(s => s.CompanyId == companyId)
                .OrderByDescending(s => s.PeriodEndDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (active is null)
            {
                throw new InvalidOperationException(
                    $"Initial backfill required. CompanyId={companyId}");
            }

            if (active.FrozenAt.HasValue)
            {
                throw new InvalidOperationException(
                    $"No active snapshot. CompanyId={companyId}");
            }

            int startIndex = Array.FindIndex(
                periods,
                p => p.PeriodEndDate == active.PeriodEndDate);

            if (startIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Active reporting period not found. CompanyId={companyId}");
            }

            int processed = 0;

            for (int i = startIndex; i < periods.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var period = periods[i];

                // The previous period is valued immediately
                // before the next report's first publication.
                DateOnly valuationDate =
                    i + 1 < periods.Length
                        ? DateOnly.FromDateTime(
                            periods[i + 1].FirstPublishedAt)
                            .AddDays(-1)
                        : evaluationDate;

                if (valuationDate <
                    DateOnly.FromDateTime(period.FirstPublishedAt))
                {
                    throw new InvalidOperationException(
                        $"Invalid publication chronology. " +
                        $"CompanyId={companyId}, " +
                        $"Period={period.PeriodEndDate}");
                }

                var report = period.Revisions
                    .Where(r =>
                        DateOnly.FromDateTime(
                            r.PublishDateTime!.Value) <= valuationDate)
                    .OrderByDescending(r => r.PublishDateTime)
                    .ThenByDescending(r => r.TracingNo)
                    .FirstOrDefault();

                if (report is null)
                {
                    throw new InvalidOperationException(
                        $"No valid report revision. " +
                        $"CompanyId={companyId}, " +
                        $"Period={period.PeriodEndDate}");
                }
                // Calculate publication-time valuation only for new periods.
                PortfolioValuationAggregate? publicationValuation = null;

                if (i > startIndex)
                {
                    var firstRevision = period.Revisions
                        .OrderBy(r => r.PublishDateTime!.Value)
                        .ThenBy(r => r.TracingNo)
                        .First();

                    DateOnly publicationDate =
                        DateOnly.FromDateTime(period.FirstPublishedAt);

                    // Use the latest available trading day before publication.
                    DateOnly publicationPriceDate =
                        publicationDate.AddDays(-1);

                    bool hasInitialPositions =
                        await dbContext.InvestmentPortfolioPositions
                            .AsNoTracking()
                            .AnyAsync(p =>
                                p.ParentCompanyId == companyId &&
                                p.PeriodEndDate == period.PeriodEndDate &&
                                p.DisclosureId == firstRevision.DisclosureId &&
                                p.SourceType == PortfolioSourceType.MonthlyActivity,
                                cancellationToken);

                    if (hasInitialPositions)
                    {
                        publicationValuation =
                            await calculatorService.CalculateAsync(
                                companyId,
                                period.PeriodEndDate,
                                firstRevision.DisclosureId,
                                publicationPriceDate,
                                cancellationToken);
                    }
                }
                var valuation =
                    await calculatorService.CalculateAsync(
                        companyId,
                        period.PeriodEndDate,
                        report.DisclosureId,
                        valuationDate,
                        cancellationToken);

                await snapshotService.UpsertAsync(
                    companyId: companyId,
                    periodEndDate: period.PeriodEndDate,
                    disclosureId: report.DisclosureId,
                    valuation: valuation,
                    valuationAsOfDate: valuationDate,
                    publicationValuation: publicationValuation,
                    cancellationToken: cancellationToken);
                processed++;
            }

            await transaction.CommitAsync(cancellationToken);

            return processed;
        });
    }
}

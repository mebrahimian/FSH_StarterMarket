
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using FSH.Modules.MarketIntelligence.Services.Calculations;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed class PortfolioValuationBackfillService(
    MarketIntelligenceDbContext dbContext,
    PortfolioValuationEligibilityService eligibilityService,
    PortfolioValuationReportCalculatorService calculatorService,
    PortfolioValuationSnapshotService snapshotService,
    ILogger<PortfolioValuationBackfillService> logger)
{

    public async Task<int> RunAsync(
        DateOnly evaluationDate,
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        const string startPeriod = "1405/01/01";

        if (companyId.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                companyId.Value);
        }

        // Eligible companies:
        // At least one monthly portfolio report and one
        // valid parent share price during the past 12 months.
        HashSet<int> eligibleCompanyIds =
            await eligibilityService.GetEligibleCompanyIdsAsync(
                evaluationDate,
                cancellationToken);

        if (companyId.HasValue)
        {
            eligibleCompanyIds.IntersectWith(
                new[] { companyId.Value });
        }

        if (eligibleCompanyIds.Count == 0)
        {
            return 0;
        }

        // Read all report revisions only once.
        var revisions =
            await PortfolioValuationReportSelector
                .GetReportRevisionsAsync(
                    dbContext,
                    cancellationToken);

        var availableRevisions = revisions
            .Where(r =>
                eligibleCompanyIds.Contains(r.CompanyId) &&
                r.PublishDateTime.HasValue &&
                DateOnly.FromDateTime(
                    r.PublishDateTime.Value) <= evaluationDate)
            .ToList();

        int processed = 0;
        List<int> failedCompanyIds = [];

        foreach (var companyGroup in availableRevisions
            .GroupBy(r => r.CompanyId)
            .OrderBy(g => g.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int companyProcessed = 0;

            try
            {
                var strategy = dbContext.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    dbContext.ChangeTracker.Clear();
                    companyProcessed = 0;

                    await using var transaction =
                        await dbContext.Database.BeginTransactionAsync(
                            cancellationToken);
                    await CalculationCompanyLock.AcquireAsync(
                        dbContext,
                        companyGroup.Key,
                        "PortfolioValuation",
                        cancellationToken);

                    var periods = companyGroup
                    .GroupBy(r => r.PeriodEndDate)
                    .OrderBy(g => g.Key, StringComparer.Ordinal)
                    .Select(g => new
                    {
                        PeriodEndDate = g.Key,
                        FirstPublishedAt = g.Min(
                            r => r.PublishDateTime!.Value),
                        Revisions = g.ToArray()
                    })
                    .ToList();

                if (periods.Count > 0)
                {
                    int firstIndex = periods.FindIndex(p =>
                        string.CompareOrdinal(
                            p.PeriodEndDate,
                            startPeriod) >= 0);

                    int beginIndex = firstIndex < 0
                        ? periods.Count - 1
                        : Math.Max(0, firstIndex - 1);

                    for (int i = beginIndex; i < periods.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var period = periods[i];

                        DateOnly valuationDate =
                            i + 1 < periods.Count
                                ? DateOnly.FromDateTime(
                                    periods[i + 1].FirstPublishedAt)
                                    .AddDays(-1)
                                : evaluationDate;

                        if (valuationDate <
                            DateOnly.FromDateTime(
                                period.FirstPublishedAt))
                        {
                            throw new InvalidOperationException(
                                $"Invalid publication chronology: " +
                                $"CompanyId={companyGroup.Key}, " +
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
                                $"No valid revision: " +
                                $"CompanyId={companyGroup.Key}, " +
                                $"Period={period.PeriodEndDate}");
                        }

                        var existing = await dbContext
                            .PortfolioValuationSnapshots
                            .AsNoTracking()
                            .Where(x =>
                                x.CompanyId == report.CompanyId &&
                                x.PeriodEndDate == report.PeriodEndDate)
                            .Select(x => new
                            {
                                x.CurrentDisclosureId,
                                x.FrozenAt
                            })
                            .FirstOrDefaultAsync(cancellationToken);

                        if (existing?.FrozenAt is not null)
                        {
                            if (existing.CurrentDisclosureId !=
                                report.DisclosureId)
                            {
                                throw new InvalidOperationException(
                                    $"Historical revision requires rebuild: " +
                                    $"CompanyId={report.CompanyId}, " +
                                    $"Period={report.PeriodEndDate}");
                            }

                            continue;
                        }

                        var valuation =
                            await calculatorService.CalculateAsync(
                                report.CompanyId,
                                report.PeriodEndDate,
                                report.DisclosureId,
                                valuationDate,
                                cancellationToken);

                        await snapshotService.UpsertAsync(
                            report.CompanyId,
                            report.PeriodEndDate,
                            report.DisclosureId,
                            valuation,
                            valuationDate,
                            cancellationToken);

                        companyProcessed++;
                    }
                }

                    await transaction.CommitAsync(cancellationToken);

                });
                processed += companyProcessed;
            }
            catch (InvalidOperationException ex)
            {
                failedCompanyIds.Add(companyGroup.Key);

                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(
                        ex,
                        "Portfolio valuation backfill failed. CompanyId={CompanyId}",
                        companyGroup.Key);
                }
                if (companyId.HasValue)
                {
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                failedCompanyIds.Add(companyGroup.Key);

                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(
                        ex,
                        "Portfolio valuation backfill failed. CompanyId={CompanyId}",
                        companyGroup.Key);
                }
                if (companyId.HasValue)
                {
                    throw;
                }
            }
            catch (DbException ex)
            {
                failedCompanyIds.Add(companyGroup.Key);

                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(
                        ex,
                        "Portfolio valuation backfill failed. CompanyId={CompanyId}",
                        companyGroup.Key);
                }
                if (companyId.HasValue)
                {
                    throw;
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            finally
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return processed;
    }
}

using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed record PortfolioValuationCompanyError(
    int CompanyId,
    string Error);

public sealed record PortfolioValuationBatchResult(
    int EligibleCompanies,
    int UpdatedCompanies,
    int BackfilledCompanies,
    int SkippedCompanies,
    int FailedCompanies,
    int ProcessedPeriods,
    IReadOnlyList<PortfolioValuationCompanyError> Errors);

public sealed class PortfolioValuationBatchIncrementalService(
    MarketIntelligenceDbContext dbContext,
    PortfolioValuationEligibilityService eligibilityService,
    PortfolioValuationIncrementalService incrementalService,
    PortfolioValuationBackfillService backfillService,
    ILogger<PortfolioValuationBatchIncrementalService> logger)
{
    public async Task<PortfolioValuationBatchResult> RunAsync(
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default)
    {
        var eligible = await eligibilityService.GetEligibleCompanyIdsAsync(
            evaluationDate,
            cancellationToken);

        int[] companyIds = eligible
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (companyIds.Length == 0)
        {
            return new PortfolioValuationBatchResult(
                0, 0, 0, 0, 0, 0, []);
        }

        IReadOnlyList<PortfolioValuationReportRef> revisions =
            await PortfolioValuationReportSelector.GetReportRevisionsAsync(
                dbContext,
                cancellationToken);

        int[] existingIds = await dbContext.PortfolioValuationSnapshots
            .AsNoTracking()
            .Where(x => companyIds.Contains(x.CompanyId))
            .Select(x => x.CompanyId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        HashSet<int> existingSet = existingIds.ToHashSet();

        int updated = 0;
        int backfilled = 0;
        int skipped = 0;
        int processedPeriods = 0;
        List<PortfolioValuationCompanyError> errors = [];
        void RecordFailure(int companyId, Exception exception)
        {
            errors.Add(new PortfolioValuationCompanyError(
                companyId,
                exception.Message));

            logger.LogError(
                exception,
                "Portfolio Valuation incremental failed. CompanyId={CompanyId}",
                companyId);
        }
        foreach (int companyId in companyIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                dbContext.ChangeTracker.Clear();

                if (existingSet.Contains(companyId))
                {
                    int count = await incrementalService.RunAsync(
                        evaluationDate,
                        companyId,
                        revisions,
                        cancellationToken);

                    if (count == 0)
                    {
                        skipped++;
                    }
                    else
                    {
                        updated++;
                        processedPeriods += count;
                    }
                }
                else
                {
                    await backfillService.RunAsync(
                        evaluationDate,
                        companyId,
                        cancellationToken);

                    int count = await dbContext.PortfolioValuationSnapshots
                        .AsNoTracking()
                        .CountAsync(
                            x => x.CompanyId == companyId,
                            cancellationToken);

                    if (count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Backfill created no snapshots. CompanyId={companyId}");
                    }

                    backfilled++;
                    processedPeriods += count;
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (InvalidOperationException exception)
            {
                RecordFailure(companyId, exception);
            }
            catch (DbUpdateException exception)
            {
                RecordFailure(companyId, exception);
            }
            catch (System.Data.Common.DbException exception)
            {
                RecordFailure(companyId, exception);
            }
        }
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
            "Portfolio Valuation batch completed. Eligible={Eligible}, Updated={Updated}, Backfilled={Backfilled}, Skipped={Skipped}, Failed={Failed}, ProcessedPeriods={ProcessedPeriods}",
            companyIds.Length,
            updated,
            backfilled,
            skipped,
            errors.Count,
            processedPeriods);
        }
        return new PortfolioValuationBatchResult(
            companyIds.Length,
            updated,
            backfilled,
            skipped,
            errors.Count,
            processedPeriods,
            errors);
    }
}
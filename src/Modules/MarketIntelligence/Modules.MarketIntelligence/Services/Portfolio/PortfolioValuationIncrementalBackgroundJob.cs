using Hangfire;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed class PortfolioValuationIncrementalBackgroundJob(
    PortfolioValuationBatchIncrementalService batchService,
    ILogger<PortfolioValuationIncrementalBackgroundJob> logger)
{
    [Queue("portfolio")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAllAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset iranNow =
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTimeOffset.UtcNow,
                "Iran Standard Time");

        DateOnly evaluationDate =
            DateOnly.FromDateTime(iranNow.DateTime);

        PortfolioValuationBatchResult result =
            await batchService.RunAsync(
                evaluationDate,
                cancellationToken);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
            "Portfolio incremental completed. Eligible={Eligible}, Updated={Updated}, Backfilled={Backfilled}, Skipped={Skipped}, Failed={Failed}, Periods={Periods}",
            result.EligibleCompanies,
            result.UpdatedCompanies,
            result.BackfilledCompanies,
            result.SkippedCompanies,
            result.FailedCompanies,
            result.ProcessedPeriods);
        }
        if (result.FailedCompanies > 0)
        {
            logger.LogWarning(
                "Portfolio incremental completed with {Failed} company errors.",
                result.FailedCompanies);
        }
    }
}
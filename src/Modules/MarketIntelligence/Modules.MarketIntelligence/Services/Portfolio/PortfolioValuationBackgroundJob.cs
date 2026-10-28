
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed class PortfolioValuationBackgroundJob(
    PortfolioValuationBackfillService backfillService,
    ILogger<PortfolioValuationBackgroundJob> logger)
{
    [Queue("portfolio")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAllAsync(
        CancellationToken cancellationToken)
    {
        TimeZoneInfo iranTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "Iran Standard Time");

        DateTime iranNow =
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                iranTimeZone);

        DateOnly evaluationDate =
            DateOnly.FromDateTime(iranNow);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
            "Portfolio valuation backfill started. Date={Date}",
            evaluationDate);
        }
        int processed = await backfillService.RunAsync(
            evaluationDate,
            cancellationToken: cancellationToken);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
            "Portfolio valuation backfill completed. Processed={Count}",
            processed);
        }
    }
}

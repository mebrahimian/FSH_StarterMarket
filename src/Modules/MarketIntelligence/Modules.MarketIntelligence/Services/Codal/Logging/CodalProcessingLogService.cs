using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Logging;

public sealed class CodalProcessingLogService(
    IServiceScopeFactory scopeFactory,
    ILogger<CodalProcessingLogService> logger)
{
    public async Task<bool> TryLogAsync(
        string operation,
        string stage,
        byte level,
        Exception? exception = null,
        string? status = null,
        string? jobId = null,
        string? fromDate = null,
        string? toDate = null,
        int? pageNumber = null,
        long? tracingNo = null,
        int? attemptCount = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<MarketIntelligenceDbContext>();

            var log = new CodalProcessingLog(
                operation: operation,
                stage: stage,
                level: level,
                status: status,
                jobId: jobId,
                fromDate: fromDate,
                toDate: toDate,
                pageNumber: pageNumber,
                tracingNo: tracingNo,
                errorType: exception?.GetType().FullName,
                errorMessage: exception?.Message[..Math.Min(
                    exception.Message.Length, 2000)],
                details: exception?.ToString(),
                attemptCount: attemptCount);

            dbContext.CodalProcessingLogs.Add(log);

            await dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(
                ex,
                "Failed to persist Codal processing log. Operation={Operation}, Stage={Stage}, JobId={JobId}",
                operation, stage, jobId);

            return false;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            logger.LogError(
                ex,
                "SQL error while persisting Codal processing log. Operation={Operation}, Stage={Stage}, JobId={JobId}",
                operation, stage, jobId);

            return false;
        }
    }
    public async Task<long> CreateProgressAsync(
    string fromDate,
    string toDate,
    CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<MarketIntelligenceDbContext>();

        var log = new CodalProcessingLog(
            operation: "Historical",
            stage: "Progress",
            level: 1,
            status: "Processing",
            fromDate: fromDate,
            toDate: toDate);

        dbContext.CodalProcessingLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        return log.Id;
    }

    public async Task UpdateProgressAsync(
        long progressId,
        string fromDate,
        string toDate,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<MarketIntelligenceDbContext>();

        var log = await dbContext.CodalProcessingLogs
            .SingleAsync(x => x.Id == progressId, cancellationToken);

        log.UpdateProgress(fromDate, toDate, pageNumber);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteProgressAsync(
        long progressId,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<MarketIntelligenceDbContext>();

        var log = await dbContext.CodalProcessingLogs
            .SingleAsync(x => x.Id == progressId, cancellationToken);

        log.CompleteProgress();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
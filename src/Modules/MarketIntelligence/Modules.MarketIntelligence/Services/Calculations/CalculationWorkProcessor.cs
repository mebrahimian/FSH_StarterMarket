using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed class CalculationWorkProcessor(
    CalculationWorkClaimService claimService,
    CalculationTaskExecutor executor,
    CalculationLeaseHeartbeat heartbeat,
    CalculationWorkCompletionService completionService)
{
    public async Task<bool> ProcessNextAsync(
        CancellationToken cancellationToken)
    {
        CalculationWorkClaim? claim =
            await claimService.TryClaimNextAsync(
                cancellationToken);

        if (claim is null)
        {
            return false;
        }

        try
        {
            using var processingCts =
    CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken);

            using var heartbeatCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            Task heartbeatTask = heartbeat.RunAsync(
                claim,
                heartbeatCts.Token);

            try
            {
                Task calculationTask = executor.ExecuteAsync(
                    claim,
                    processingCts.Token);

                Task completedTask = await Task.WhenAny(
                    calculationTask,
                    heartbeatTask);

                if (completedTask == heartbeatTask)
                {
                    await processingCts.CancelAsync();

                    try
                    {
                        await calculationTask;
                    }
                    catch (OperationCanceledException)
                        when (processingCts.IsCancellationRequested)
                    {
                        // Calculation cancelled after heartbeat stopped.
                    }

                    await heartbeatTask;

                    throw new InvalidOperationException(
                        $"Calculation heartbeat stopped unexpectedly. " +
                        $"WorkItemId={claim.Id}");
                }

                await calculationTask;
            }
            finally
            {
                await heartbeatCts.CancelAsync();
                await heartbeatTask;
            }
        }
        catch (InvalidOperationException ex)
        {
            await RecordFailureAsync(claim, ex, cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
        {
            await RecordFailureAsync(claim, ex, cancellationToken);
            return true;
        }
        catch (DbException ex)
        {
            await RecordFailureAsync(claim, ex, cancellationToken);
            return true;
        }
        catch (TimeoutException ex)
        {
            await RecordFailureAsync(claim, ex, cancellationToken);
            return true;
        }

        await completionService.MarkSucceededAsync(
            claim,
            cancellationToken);

        return true;
    }

    private Task RecordFailureAsync(
        CalculationWorkClaim claim,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return completionService.MarkFailedAsync(
            claim,
            exception.ToString(),
            DateTimeOffset.UtcNow.AddMinutes(5),
            cancellationToken);
    }
}
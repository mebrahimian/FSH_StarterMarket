using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed class CalculationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CalculationWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope =
                    scopeFactory.CreateAsyncScope();

                var processor = scope.ServiceProvider
                    .GetRequiredService<CalculationWorkProcessor>();

                bool processed = await processor.ProcessNextAsync(
                    stoppingToken);

                if (!processed)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (DbUpdateException ex)
            {
                await HandleErrorAsync(ex, stoppingToken);
            }
            catch (DbException ex)
            {
                await HandleErrorAsync(ex, stoppingToken);
            }
            catch (InvalidOperationException ex)
            {
                await HandleErrorAsync(ex, stoppingToken);
            }
            catch (TimeoutException ex)
            {
                await HandleErrorAsync(ex, stoppingToken);
            }
        }
    }

    private async Task HandleErrorAsync(
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Calculation worker failed.");

        await Task.Delay(
            TimeSpan.FromSeconds(15),
            cancellationToken);
    }
}
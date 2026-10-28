using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed class CalculationLeaseHeartbeat(
    IServiceScopeFactory scopeFactory)
{
    public async Task RunAsync(
    CalculationWorkClaim claim,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(5));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await using var scope =
                    scopeFactory.CreateAsyncScope();

                var leaseService = scope.ServiceProvider
                    .GetRequiredService<CalculationWorkLeaseService>();

                bool renewed = await leaseService.RenewAsync(
                    claim,
                    cancellationToken);

                if (!renewed)
                {
                    throw new InvalidOperationException(
                        $"Calculation lease lost. WorkItemId={claim.Id}");
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Normal heartbeat shutdown.
        }
    }
}
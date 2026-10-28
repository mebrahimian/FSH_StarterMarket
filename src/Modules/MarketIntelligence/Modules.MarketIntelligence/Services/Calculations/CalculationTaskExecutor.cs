namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public sealed class CalculationTaskExecutor(
    IEnumerable<ICalculationTaskHandler> handlers)
{
    public Task ExecuteAsync(
        CalculationWorkClaim claim,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);

        ICalculationTaskHandler handler =
            handlers.SingleOrDefault(x =>
                x.TaskType == claim.TaskType)
            ?? throw new InvalidOperationException(
                $"Calculation handler not found: {claim.TaskType}");

        return handler.ExecuteAsync(
            claim.CompanyId,
            cancellationToken);
    }
}
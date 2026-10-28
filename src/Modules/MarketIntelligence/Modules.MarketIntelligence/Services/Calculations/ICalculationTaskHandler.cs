namespace FSH.Modules.MarketIntelligence.Services.Calculations;

public interface ICalculationTaskHandler
{
    string TaskType { get; }

    Task ExecuteAsync(
        int companyId,
        CancellationToken cancellationToken);
}
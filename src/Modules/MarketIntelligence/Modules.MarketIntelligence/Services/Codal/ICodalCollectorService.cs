namespace FSH.Modules.MarketIntelligence.Service.Codal;

public interface ICodalCollectorService
{
    Task CollectAsync(
        CancellationToken cancellationToken = default);
}
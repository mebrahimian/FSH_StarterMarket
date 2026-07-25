namespace FSH.Modules.MarketIntelligence.Services.Codal;

public interface ICodalCollectorService
{
    Task CollectAsync(
        CancellationToken cancellationToken = default);
}
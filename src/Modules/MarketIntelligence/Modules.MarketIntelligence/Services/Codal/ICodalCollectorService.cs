namespace FSH.Modules.MarketIntelligence.Services.Codal;

public interface ICodalCollectorService
{
    Task CollectAsync(
        CancellationToken cancellationToken = default);
    Task CollectAsync2(
        CancellationToken cancellationToken = default);
}
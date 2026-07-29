namespace FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

public interface ICodalCollectorService
{
    Task CollectAsync2(
        CancellationToken cancellationToken = default);
}
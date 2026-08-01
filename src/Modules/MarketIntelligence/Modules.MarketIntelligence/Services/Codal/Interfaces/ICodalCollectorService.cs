namespace FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

public interface ICodalCollectorService
{
    Task CollectBackfillAsync(
        CancellationToken cancellationToken = default);
    Task ParsePendingDisclosuresAsync(
        CancellationToken cancellationToken = default);
    Task CollectIncrementalAsync(CancellationToken cancellationToken = default);
}
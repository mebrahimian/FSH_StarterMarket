namespace FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

public interface ICodalCollectorService
{
    //Task CollectBackfillAsync(CancellationToken cancellationToken = default);
    Task CollectSymbolBackfillAsync(string symbol, string fromDate,  string toDate, CancellationToken cancellationToken =
        default);
    Task<bool> ParsePendingDisclosuresAsync(CancellationToken cancellationToken = default);
    Task CollectIncrementalAsync(CancellationToken cancellationToken = default);
    Task CollectBackfillChunkAsync(int startPage,  int endPage, 
        CancellationToken cancellationToken = default);
}
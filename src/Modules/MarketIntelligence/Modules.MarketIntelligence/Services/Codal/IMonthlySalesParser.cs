namespace FSH.Modules.MarketIntelligence.Services.Codal;

public interface IMonthlySalesParser
{
    Task<MonthlySalesParseResult?> ParseAsync(
        string datasourceJson,
        CancellationToken cancellationToken = default);
}
using FSH.Modules.MarketIntelligence.Services.Codal;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

public interface IMonthlySalesParser
{
    Task<MonthlySalesParseResult?> ParseAsync(
        string datasourceJson,
        CancellationToken cancellationToken = default);
}
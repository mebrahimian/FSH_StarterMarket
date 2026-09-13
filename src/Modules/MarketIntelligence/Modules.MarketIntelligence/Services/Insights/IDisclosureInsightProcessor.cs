using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Services.Insights;

public interface IDisclosureInsightProcessor
{
    bool CanProcess(Disclosure disclosure);

    Task ProcessAsync(
        Disclosure disclosure,
        CancellationToken cancellationToken = default);
}
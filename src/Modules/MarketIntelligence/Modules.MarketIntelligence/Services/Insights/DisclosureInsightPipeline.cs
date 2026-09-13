using FSH.Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Services.Insights;

public sealed class DisclosureInsightPipeline(
    IEnumerable<IDisclosureInsightProcessor> processors)
{
    private readonly IReadOnlyList<IDisclosureInsightProcessor> _processors =
        processors.ToArray();

    public async Task ProcessAsync(Disclosure disclosure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        IReadOnlyList<IDisclosureInsightProcessor> applicableProcessors =
            _processors
                .Where(x => x.CanProcess(disclosure))
                .ToList();

        foreach (IDisclosureInsightProcessor processor in applicableProcessors)
        {
            await processor.ProcessAsync(
                disclosure,
                cancellationToken);
        }
    }
}
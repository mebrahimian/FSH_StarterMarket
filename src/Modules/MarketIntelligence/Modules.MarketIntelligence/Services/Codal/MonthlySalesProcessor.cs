using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Processors;

public sealed class MonthlySalesProcessor : ICodalDisclosureProcessor
{
    public bool CanProcess(Disclosure disclosure)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        return disclosure.Let == 58
            && disclosure.Rt == 0
            && disclosure.SalesParseStatus == DisclosureParseStatus.Pending;
    }

    public Task ProcessAsync(
        Disclosure disclosure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}
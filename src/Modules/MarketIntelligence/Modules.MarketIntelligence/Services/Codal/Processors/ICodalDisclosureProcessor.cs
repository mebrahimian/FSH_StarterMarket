using FSH.Modules.MarketIntelligence.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Processors;

public interface ICodalDisclosureProcessor
{
    bool CanProcess(Disclosure disclosure);

    Task ProcessAsync(
        Disclosure disclosure,
        CancellationToken cancellationToken = default);
}
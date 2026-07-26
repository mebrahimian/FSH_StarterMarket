using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public interface IMonthlySalesParser
{
    Task<MonthlySalesParseResult?> ParseAsync(
        string url,
        CancellationToken cancellationToken);
}

using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed record MonthlySalesParseResult(
    decimal SaleMonthly,
    decimal SaleYearly);

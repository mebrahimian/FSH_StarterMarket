using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed record CodalUrlInfo(
    short? Let,
    byte? Rt,
    byte? Ct,
    short? Ft);

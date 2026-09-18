using FSH.Framework.Core.Domain;
using System;

namespace Modules.MarketIntelligence.Domain;

public sealed class TsetmcInstrument
    : BaseEntity<int>, IGlobalEntity
{
    public string InsCode { get; set; } = default!;

    public string Isin { get; set; } = default!;

    public string Symbol { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string YVal { get; set; } = default!;

    public DateTimeOffset LastSeenAt { get; set; }
}
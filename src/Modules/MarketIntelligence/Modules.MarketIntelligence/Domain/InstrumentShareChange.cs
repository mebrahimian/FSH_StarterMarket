using System.Diagnostics.CodeAnalysis;
using FSH.Framework.Core.Domain;

namespace Modules.MarketIntelligence.Domain;

[SuppressMessage(
    "CodeQuality",
    "S1144",
    Justification = "Private setters are required by EF Core materialization.")]
public sealed class InstrumentShareChange :
    BaseEntity<int>,
    IGlobalEntity
{
    private InstrumentShareChange()
    {
    }

    public InstrumentShareChange(
        int instrumentId,
        DateOnly effectiveDate,
        long oldShares,
        long newShares)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(instrumentId);
        ArgumentOutOfRangeException.ThrowIfNegative(oldShares);
        ArgumentOutOfRangeException.ThrowIfNegative(newShares);

        InstrumentId = instrumentId;
        EffectiveDate = effectiveDate;
        OldShares = oldShares;
        NewShares = newShares;
    }

    public int InstrumentId { get; private set; }

    public DateOnly EffectiveDate { get; private set; }

    public long OldShares { get; private set; }

    public long NewShares { get; private set; }
}
using System.Diagnostics.CodeAnalysis;
using FSH.Framework.Core.Domain;

namespace Modules.MarketIntelligence.Domain;

[SuppressMessage(
    "CodeQuality",
    "S1144",
    Justification = "Private setters are required by EF Core materialization.")]
public sealed class InstrumentValuationHistory :
    BaseEntity<int>,
    IGlobalEntity
{
    private InstrumentValuationHistory()
    {
    }

    public InstrumentValuationHistory(
        int instrumentId,
        DateOnly observedDate,
        decimal? eps,
        decimal? pe,
        decimal? sectorPE,
        decimal? salesPerShare)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(instrumentId);

        InstrumentId = instrumentId;
        ObservedDate = observedDate;
        Eps = eps;
        PE = pe;
        SectorPE = sectorPE;
        SalesPerShare = salesPerShare;
    }

    public int InstrumentId { get; private set; }

    public DateOnly ObservedDate { get; private set; }

    public decimal? Eps { get; private set; }

    public decimal? PE { get; private set; }

    public decimal? SectorPE { get; private set; }

    public decimal? SalesPerShare { get; private set; }
    public void Update(
    decimal? eps,
    decimal? pe,
    decimal? sectorPe,
    decimal? salesPerShare)
    {
        Eps = eps;
        PE = pe;
        SectorPE = sectorPe;
        SalesPerShare = salesPerShare;
    }
    public void UpdateMarketWatch(
    decimal? eps,
    decimal? pe)
    {
        Eps = eps;
        PE = pe;
    }
}
using FSH.Framework.Core.Domain;

namespace Modules.MarketIntelligence.Domain;

public sealed class CapitalIncreaseEvent :
    BaseEntity<int>,
    IGlobalEntity
{
    private CapitalIncreaseEvent()
    {
    }

    public CapitalIncreaseEvent(
     int instrumentId,
     long oldShares,
     long newShares,
     DateOnly adjustmentEffectiveDate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(instrumentId);

        if (oldShares <= 0 || newShares <= oldShares)
        {
            throw new ArgumentException(
                "Capital increase must have NewShares greater than OldShares.");
        }

        InstrumentId = instrumentId;
        OldShares = oldShares;
        NewShares = newShares;
        AdjustmentEffectiveDate = adjustmentEffectiveDate;
    }

    public int InstrumentId { get; private set; }

    // TSETMC - logical capital increase
    public long OldShares { get; private set; }
    public long NewShares { get; private set; }

    // Latest effective date among related share changes
    public DateOnly AdjustmentEffectiveDate { get; private set; }

    // Codal
    public Guid? CodalDisclosureId { get; private set; }

    public DateOnly? AssemblyDate { get; private set; }
    public DateOnly? RegistrationDate { get; private set; }

    // Capital amounts in million IRR
    public decimal? PreviousCapital { get; private set; }
    public decimal? NewCapital { get; private set; }

    // Capital increase sources
    public decimal? CashIncoming { get; private set; }
    public decimal? RetainedEarnings { get; private set; }
    public decimal? Reserves { get; private set; }
    public decimal? SharePremium { get; private set; }
    public decimal? RevaluationSurplus { get; private set; }
    public void UpdateAdjustmentEffectiveDate(DateOnly effectiveDate)
    {
        if (effectiveDate > AdjustmentEffectiveDate)
        {
            AdjustmentEffectiveDate = effectiveDate;
        }
    }

    public void ApplyCodalRegistration(
        Guid disclosureId,
        DateOnly? assemblyDate,
        DateOnly? registrationDate,
        decimal? previousCapital,
        decimal? newCapital,
        decimal? cashIncoming,
        decimal? retainedEarnings,
        decimal? reserves,
        decimal? sharePremium,
        decimal? revaluationSurplus)
    {
        if (disclosureId == Guid.Empty)
        {
            throw new ArgumentException(
                "DisclosureId cannot be empty.",
                nameof(disclosureId));
        }

        if (CodalDisclosureId.HasValue &&
            CodalDisclosureId.Value != disclosureId)
        {
            throw new InvalidOperationException(
                "Capital increase is already linked to another disclosure.");
        }

        CodalDisclosureId = disclosureId;
        AssemblyDate = assemblyDate;
        RegistrationDate = registrationDate;

        PreviousCapital = previousCapital;
        NewCapital = newCapital;

        CashIncoming = cashIncoming;
        RetainedEarnings = retainedEarnings;
        Reserves = reserves;
        SharePremium = sharePremium;
        RevaluationSurplus = revaluationSurplus;
    }
}
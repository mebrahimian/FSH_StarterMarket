using System.Diagnostics.CodeAnalysis;
using FSH.Framework.Core.Domain;

namespace FSH.Modules.MarketIntelligence.Domain;

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

    // Codal capital increase registration
    public Guid? CodalDisclosureId { get; private set; }

    public DateOnly? AssemblyDate { get; private set; }

    public DateOnly? RegistrationDate { get; private set; }

    // Capital amounts in million IRR
    public decimal? PreviousCapital { get; private set; }

    public decimal? NewCapital { get; private set; }

    // Capital increase sources in million IRR

    public decimal? CashIncoming { get; private set; }

    public decimal? RetainedEarnings { get; private set; }

    public decimal? Reserves { get; private set; }

    public decimal? SharePremium { get; private set; }

    public decimal? RevaluationSurplus { get; private set; }

    public void ApplyCodalRegistration(
        CapitalIncreaseRegistrationDetails registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (registration.DisclosureId == Guid.Empty)
        {
            throw new ArgumentException(
                "Codal DisclosureId is required.",
                nameof(registration));
        }

        if (CodalDisclosureId.HasValue &&
            CodalDisclosureId.Value != registration.DisclosureId)
        {
            throw new InvalidOperationException(
                "This share change is already linked to another Codal disclosure.");
        }

        if (registration.PreviousCapital <= 0 ||
            registration.NewCapital <= registration.PreviousCapital)
        {
            throw new ArgumentException(
                "Invalid registered capital amounts.",
                nameof(registration));
        }

        CodalDisclosureId = registration.DisclosureId;
        AssemblyDate = registration.AssemblyDate;
        RegistrationDate = registration.RegistrationDate;

        PreviousCapital = registration.PreviousCapital;
        NewCapital = registration.NewCapital;

        CashIncoming = registration.CashIncoming;
        RetainedEarnings = registration.RetainedEarnings;
        Reserves = registration.Reserves;
        SharePremium = registration.SharePremium;
        RevaluationSurplus = registration.RevaluationSurplus;
    }

    public sealed record CapitalIncreaseRegistrationDetails(
        Guid DisclosureId,
        DateOnly AssemblyDate,
        DateOnly RegistrationDate,
        decimal PreviousCapital,
        decimal NewCapital,
        decimal? CashIncoming,
        decimal? RetainedEarnings,
        decimal? Reserves,
        decimal? SharePremium,
        decimal? RevaluationSurplus);


}
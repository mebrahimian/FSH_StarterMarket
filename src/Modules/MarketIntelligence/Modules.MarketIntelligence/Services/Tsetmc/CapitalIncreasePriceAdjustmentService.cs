
using FSH.Modules.MarketIntelligence.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public enum PriceAdjustmentStatus
{
    NoCapitalIncrease = 0,
    ShareRatioAdjusted = 1,
    CashContributionNeedsReview = 2,
    IncompleteCodalSource = 3
}

public sealed record AppliedCapitalIncrease(
    DateOnly EffectiveDate,
    long OldShares,
    long NewShares,
    decimal Factor,
    Guid? CodalDisclosureId,
    decimal? CashIncoming,
    decimal? SharePremium);

public sealed record PriceAdjustmentResult(
    decimal RawPriceChangePercent,
    decimal? AdjustedPriceChangePercent,
    decimal? AdjustedStartPrice,
    decimal AdjustmentFactor,
    PriceAdjustmentStatus Status,
    IReadOnlyList<AppliedCapitalIncrease> Events);

public sealed class CapitalIncreasePriceAdjustmentService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<PriceAdjustmentResult> CalculateAsync(
        int instrumentId,
        DateOnly startDate,
        DateOnly endDate,
        long startPrice,
        long endPrice,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(instrumentId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(startPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(endPrice);

        if (startDate >= endDate)
        {
            throw new ArgumentException(
                "StartDate must be earlier than EndDate.");
        }

        var events = await dbContext.CapitalIncreaseEvents
            .AsNoTracking()
            .Where(x =>
                x.InstrumentId == instrumentId &&
                x.AdjustmentEffectiveDate > startDate &&
                x.AdjustmentEffectiveDate <= endDate)
            .OrderBy(x => x.AdjustmentEffectiveDate)
            .ToListAsync(cancellationToken);

        decimal rawChange =
            ((decimal)endPrice / startPrice - 1m) * 100m;

        decimal factor = 1m;

        var appliedEvents = new List<AppliedCapitalIncrease>();

        foreach (var capitalEvent in events)
        {
            decimal eventFactor =
                (decimal)capitalEvent.OldShares /
                capitalEvent.NewShares;

            factor *= eventFactor;

            appliedEvents.Add(new AppliedCapitalIncrease(
                capitalEvent.AdjustmentEffectiveDate,
                capitalEvent.OldShares,
                capitalEvent.NewShares,
                eventFactor,
                capitalEvent.CodalDisclosureId,
                capitalEvent.CashIncoming,
                capitalEvent.SharePremium));
        }

        if (events.Count == 0)
        {
            return new PriceAdjustmentResult(
                rawChange,
                rawChange,
                startPrice,
                1m,
                PriceAdjustmentStatus.NoCapitalIncrease,
                appliedEvents);
        }

        bool hasCashContribution = events.Any(x =>
            x.CashIncoming is > 0m ||
            x.SharePremium is > 0m);

        bool hasIncompleteSource = events.Any(x =>
            !x.CodalDisclosureId.HasValue ||
            !x.CashIncoming.HasValue ||
            !x.SharePremium.HasValue);

        if (hasCashContribution || hasIncompleteSource)
        {
            return new PriceAdjustmentResult(
                rawChange,
                null,
                null,
                factor,
                hasCashContribution
                    ? PriceAdjustmentStatus.CashContributionNeedsReview
                    : PriceAdjustmentStatus.IncompleteCodalSource,
                appliedEvents);
        }

        decimal adjustedStartPrice = startPrice * factor;

        decimal adjustedChange =
            ((decimal)endPrice / adjustedStartPrice - 1m) * 100m;

        return new PriceAdjustmentResult(
            rawChange,
            adjustedChange,
            adjustedStartPrice,
            factor,
            PriceAdjustmentStatus.ShareRatioAdjusted,
            appliedEvents);
    }
}
